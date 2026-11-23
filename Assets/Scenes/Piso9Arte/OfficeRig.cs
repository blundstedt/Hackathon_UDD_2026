using UnityEngine;

namespace Hackaton.Levels
{
    public enum OfficePose { Patrol, Detect, Chase, Strike, Search, Fury, Hurt, Shutdown, Summon, Fall, Rebuild, Slam }

    // Pivot positions are authored on the three prefabs. The CRT surface uses pixel coordinates
    // stored in vertex colors, so the same material also draws every chassis and expression.
    public sealed class OfficeRig
    {
        readonly Transform model, torso, head, armor;
        readonly Transform[] arms = new Transform[2], legs = new Transform[2], rotors = new Transform[3];
        readonly Renderer display;
        readonly Vector3 rest;
        readonly MaterialPropertyBlock screen = new();
        readonly OfficeActor.Kind kind;
        OfficePose last = (OfficePose)(-1);
        float clock;

        public OfficeRig(Transform root, OfficeActor.Kind kind)
        {
            this.kind = kind;
            model = root.Find("Model");
            torso = model.Find("Torso");
            head = torso.Find("Head");
            rest = torso.localPosition;
            display = head.GetComponent<Renderer>();
            armor = torso.Find("Rebuilt");
            if (armor) armor.gameObject.SetActive(false);
            arms[0] = torso.Find("ArmL"); arms[1] = torso.Find("ArmR");
            legs[0] = model.Find("LegL"); legs[1] = model.Find("LegR");
            for (int i = 0; i < rotors.Length; i++) rotors[i] = torso.Find("Rotor" + i);
            clock = Random.value * 6f;
        }

        public void Rebuild() => armor.gameObject.SetActive(true);

        public void Animate(OfficePose pose, float speed, float phase)
        {
            bool heavy = kind == OfficeActor.Kind.Corruptor;
            float stride = Mathf.Clamp01(speed / 3.5f);
            clock += Time.deltaTime * (heavy ? 3.3f + speed : 5f + speed * 1.8f);
            float wave = Mathf.Sin(clock), attack = 0f, tilt = 0f;
            if (pose == OfficePose.Strike || pose == OfficePose.Summon || pose == OfficePose.Slam)
            {
                attack = phase < .66f ? Mathf.Sin(phase / .66f * 1.57f) : (1 - phase) * -3f;
                tilt = -attack * 12f;
            }
            if (pose == OfficePose.Detect) tilt = -13f;
            if (pose == OfficePose.Hurt) tilt = -17f * (1 - phase);
            if (pose == OfficePose.Shutdown) tilt = Mathf.SmoothStep(0, 80, phase);
            if (pose == OfficePose.Fall) tilt = phase * 96f;
            if (pose == OfficePose.Rebuild) tilt = 36f * (1 - phase);
            float lift = kind == OfficeActor.Kind.Drone ? .1f * wave : Mathf.Abs(wave) * .075f * stride;
            if (kind == OfficeActor.Kind.Drone) tilt += stride * 9f;
            model.localRotation = Quaternion.Euler(tilt, 0, pose == OfficePose.Hurt ? 9f : wave * stride * 2.8f);
            if (pose == OfficePose.Shutdown && phase >= .92f)
            {
                screen.SetFloat("_Expression", 8);
                display.SetPropertyBlock(screen);
            }
            float jump = pose == OfficePose.Slam ? .72f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(phase / .7f)) : 0;
            model.localPosition = Vector3.up * jump;
            torso.localPosition = rest + Vector3.up * lift;
            head.localRotation = Quaternion.Euler(heavy ? 5f - 12f * attack : -5f * wave, Mathf.Sin(clock * .47f) *
                (pose == OfficePose.Search || pose == OfficePose.Patrol ? 25f : 6f), heavy ? -12f + wave * 4 : wave * 3);
            for (int i = 0; i < arms.Length; i++)
            {
                float swing = (i == 0 ? 1 : -1) * wave * stride;
                if (arms[i]) arms[i].localRotation = Quaternion.Euler(swing * 29f - attack * (i == 1 ? 86 : 50), 0, swing * 6);
                if (legs[i]) legs[i].localRotation = Quaternion.Euler(-swing * 34f, 0, 0);
            }
            foreach (var rotor in rotors) if (rotor) rotor.Rotate(0, 980f * Time.deltaTime, 0);
            if (pose == last) return;
            last = pose;
            screen.SetFloat("_Kind", (int)kind);
            screen.SetFloat("_Expression", pose == OfficePose.Rebuild ? 5 : pose == OfficePose.Summon || pose == OfficePose.Slam ? 3 : (int)pose);
            screen.SetFloat("_Pulse", pose == OfficePose.Fury || pose == OfficePose.Hurt ? 1f : .13f);
            display.SetPropertyBlock(screen);
        }
    }
}
