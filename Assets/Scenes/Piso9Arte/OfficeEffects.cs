using System.Collections;
using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Levels
{
    public enum ShiftSound { Hum, Hunt, Defense, Boss, Alert, Step, Glass, Crash, Pulse, Shot, Down, Clear }

    public sealed class OfficeEffects : MonoBehaviour
    {
        [SerializeField] Material signalMaterial;
        [SerializeField] AudioClip[] sounds;
        AudioSource music;
        Mesh cube;
        MaterialPropertyBlock tint;
        public static readonly Color Magenta = new(1f, .025f, .29f);
        public static readonly Color Ice = new(.14f, .75f, .83f);

        void Awake()
        {
            tint = new MaterialPropertyBlock();
            var sample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = sample.GetComponent<MeshFilter>().sharedMesh;
            sample.SetActive(false);
            Destroy(sample);
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = .24f;
            Track(ShiftSound.Hum);
        }

        public void Track(ShiftSound sound)
        {
            music.clip = sounds[(int)sound];
            music.Play();
        }

        public void Sound(ShiftSound sound, Vector3 position, float volume = .65f)
        {
            var clip = sounds[(int)sound];
            var voice = new GameObject(sound.ToString());
            voice.transform.SetParent(transform);
            voice.transform.position = position;
            var source = voice.AddComponent<AudioSource>();
            source.spatialBlend = .8f;
            source.minDistance = 4f;
            source.maxDistance = 42f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.PlayOneShot(clip, volume);
            Destroy(voice, clip.length + .05f);
        }

        public Transform Block(string label, Vector3 position, Vector3 size, Color color, bool solid = false)
        {
            var block = new GameObject(label);
            block.transform.SetParent(transform);
            block.transform.position = position;
            block.transform.localScale = size;
            block.AddComponent<MeshFilter>().sharedMesh = cube;
            var renderer = block.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = signalMaterial;
            tint.SetColor("_Tint", color);
            renderer.SetPropertyBlock(tint);
            if (solid) block.AddComponent<BoxCollider>();
            return block.transform;
        }

        public void Burst(Vector3 position, Color color, int count = 10) =>
            StartCoroutine(Scatter(position, color, count));

        IEnumerator Scatter(Vector3 position, Color color, int count)
        {
            var chips = new Transform[count];
            var velocities = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                chips[i] = Block("Fragmento", position + Random.insideUnitSphere * .3f,
                    new Vector3(.08f, Random.Range(.09f,.29f), .046f), color);
                velocities[i] = Random.insideUnitSphere * 4 + Vector3.up * 2;
            }
            for (float time = 0; time < .85f; time += Time.deltaTime)
            {
                for (int i = 0; i < count; i++)
                {
                    velocities[i] += Physics.gravity * Time.deltaTime;
                    chips[i].position += velocities[i] * Time.deltaTime;
                    chips[i].Rotate(45 * Time.deltaTime, 190 * Time.deltaTime, 64 * Time.deltaTime);
                }
                yield return null;
            }
            foreach (var chip in chips) Destroy(chip.gameObject);
        }

        public void Ring(Vector3 center, float radius, float seconds)
        {
            var ring = new GameObject("Aviso de impacto");
            ring.transform.SetParent(transform);
            var line = ring.AddComponent<LineRenderer>();
            line.sharedMaterial = signalMaterial;
            tint.SetColor("_Tint", Magenta);
            line.SetPropertyBlock(tint);
            line.loop = true;
            line.positionCount = 40;
            line.widthMultiplier = .085f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = 2 * Mathf.PI * i / line.positionCount;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, .12f, Mathf.Sin(angle) * radius));
            }
            Destroy(ring, seconds);
        }

        public void Bolt(OfficeActor owner, Vector3 muzzle, Vector3 direction, int damage)
        {
            var fragment = Block("Pulso corrupto", muzzle, new Vector3(.16f,.16f,.44f), Magenta);
            fragment.rotation = Quaternion.LookRotation(direction);
            var shot = fragment.gameObject.AddComponent<CombatProjectile>();
            shot.SetTrajectory(10.5f, 0);
            shot.Launch(owner.transform, direction, damage);
            Sound(ShiftSound.Shot, muzzle, .35f);
        }

        public void CancelShots()
        {
            foreach (var shot in GetComponentsInChildren<CombatProjectile>())
                shot.gameObject.SetActive(false);
        }
    }
}
