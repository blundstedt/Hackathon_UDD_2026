using Hackaton.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hackaton.Lobby
{
    // Each table owns its local practice match. Playing never changes the avatar or campaign.
    public sealed class PlazaTableTennis : MonoBehaviour
    {
        [SerializeField] private Transform ball, playerPaddle, machinePaddle;
        [SerializeField] private Text score, instruction;
        [SerializeField] private AudioSource speaker;
        [SerializeField] private AudioClip paddleHit, tableBounce;
        private enum Phase { Waiting, ToMachine, ToPlayer, Point, Finished }
        private Phase phase;
        private const float End = 1.68f, Width = .99f, Surface = 1.03f;
        private const int WinningScore = 5;
        private int playerPoints, machinePoints, rally;
        private float startX, targetX, travel, duration, playerX, machineX, robotTarget, pause;
        private float swing, away;
        private PlayerInputReader participant;

        private void OnEnable() => ResetMatch();
        private void ResetMatch()
        {
            participant = null;
            phase = Phase.Waiting;
            playerPoints = machinePoints = rally = 0;
            playerX = machineX = swing = away = 0f;
            ball.localPosition = new Vector3(0f, Surface + .15f, End);
            Scoreboard();
            instruction.text = "ACÉRCATE AL EXTREMO ROSA  ·  E PARA SACAR";
            PositionPaddles();
        }
        private void Scoreboard() => score.text = $"TÚ   {playerPoints}     /     {machinePoints}   IA";

        private void Update()
        {
            var camera = Camera.main;
            var input = camera ? camera.GetComponentInParent<PlayerInputReader>() : null;
            Vector3 local = input ? transform.InverseTransformPoint(input.transform.position) : Vector3.zero;
            bool atTable = input && Mathf.Abs(local.x) < Width + .95f && local.z > End - .16f &&
                           local.z < End + 2.55f && Mathf.Abs(local.y) < 1.1f;
            if (!atTable || (participant && participant != input))
            {
                bool matchStarted = phase != Phase.Waiting || rally > 0 || playerPoints + machinePoints > 0;
                if (matchStarted && (away += Time.deltaTime) > 1.25f) ResetMatch();
                else if (!matchStarted) instruction.text = "ACÉRCATE AL EXTREMO ROSA  ·  E PARA SACAR";
                return;
            }
            if (!input.HasControl) return;
            away = 0f;
            bool use = Keyboard.current?.eKey.wasPressedThisFrame == true;
            playerX = Mathf.Clamp(local.x, -Width, Width);
            swing = Mathf.MoveTowards(swing, 0f, 4.5f * Time.deltaTime);
            PositionPaddles();
            if (phase == Phase.Finished)
            {
                if (use) ResetMatch();
                return;
            }
            if (phase == Phase.Point)
            {
                if ((pause -= Time.deltaTime) <= 0f) phase = Phase.Waiting;
                return;
            }
            if (phase == Phase.Waiting)
            {
                instruction.text = "E  SACAR     ·     A / D  MOVER LA PALETA";
                ball.localPosition = new Vector3(playerX, Surface + .23f, End);
                if (use)
                {
                    participant = input;
                    rally = 0;
                    HitToMachine(playerX);
                }
                return;
            }

            travel += Time.deltaTime;
            float t = Mathf.Clamp01(travel / duration);
            bool incoming = phase == Phase.ToPlayer;
            float z = Mathf.Lerp(incoming ? -End : End, incoming ? End : -End, t);
            // Clear the net, bounce on the receiving half and rise to paddle height.
            const float bounce = .72f;
            if (travel - Time.deltaTime < duration * bounce && travel >= duration * bounce)
                speaker.PlayOneShot(tableBounce, .65f);
            float u = t < bounce ? t / bounce : (t - bounce) / (1f - bounce);
            float height = t < bounce ? .31f * (1f-u) + 2.35f*u*(1f-u) : .31f*u + .65f*u*(1f-u);
            float x = Mathf.Lerp(startX, targetX, t);
            ball.localPosition = new Vector3(x, Surface + .065f + height, z);
            if (!incoming)
            {
                if (t > .34f) machineX = Mathf.MoveTowards(machineX, robotTarget, Time.deltaTime * 1.75f);
                if (t < 1f) return;
                if (Mathf.Abs(machineX-targetX) > .33f) { Award(true); return; }
                float placement = Mathf.Sin((rally+1)*2.38f + playerPoints*.7f - machinePoints*.45f) * Width;
                speaker.PlayOneShot(paddleHit);
                Launch(Phase.ToPlayer, targetX, placement);
                instruction.text = "SIGUE LA PELOTA     ·     E  DEVOLVER";
                return;
            }
            if (use && swing <= 0f)
            {
                swing = 1f;
                if (t >= .79f && Mathf.Abs(x-playerX) <= .43f) { HitToMachine(x); return; }
            }
            if (travel > duration + .13f) Award(false);
        }

        private void PositionPaddles()
        {
            playerPaddle.localPosition = new Vector3(playerX, Surface + .27f, End + .13f - swing*.22f);
            playerPaddle.localRotation = Quaternion.Euler(-12f + swing*38f, 0f, -15f);
            machinePaddle.localPosition = new Vector3(machineX, Surface + .27f, -End - .12f);
        }
        private void Launch(Phase next, float from, float to)
        {
            phase = next; startX = from; targetX = to; travel = 0f;
            duration = Mathf.Max(.79f, 1.36f - rally*.045f);
        }
        private void HitToMachine(float contact)
        {
            speaker.PlayOneShot(paddleHit);
            swing = 1f;
            rally++;
            float placement = Mathf.Clamp((contact-playerX)*2.25f - playerX*.81f, -Width, Width);
            // Tracking drifts on long rallies. A well-angled return can beat the machine earlier.
            float error = rally < 4 ? .06f : Mathf.Min(.58f, .16f*(rally-3));
            robotTarget = Mathf.Clamp(placement + Mathf.Cos(rally*2f)*error, -Width, Width);
            Launch(Phase.ToMachine, contact, placement);
            instruction.text = "EN JUEGO";
        }
        private void Award(bool toPlayer)
        {
            if (toPlayer) playerPoints++; else machinePoints++;
            Scoreboard();
            if (playerPoints == WinningScore || machinePoints == WinningScore)
            {
                phase = Phase.Finished;
                instruction.text = (toPlayer ? "GANASTE" : "GANA LA IA") + "     ·     E  REVANCHA";
                return;
            }
            pause = 1f; phase = Phase.Point;
            instruction.text = toPlayer ? "PUNTO PARA TI" : "PUNTO PARA LA IA";
        }
    }
}
