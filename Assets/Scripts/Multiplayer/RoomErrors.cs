using System;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;

namespace Hackaton.Multiplayer
{
    internal static class RoomErrors
    {
        public static string Describe(Exception problem, string reason)
        {
            if (!string.IsNullOrEmpty(reason)) return reason;
            for (Exception cause = problem; cause != null; cause = cause.InnerException)
            {
                if (cause is not Unity.Services.Lobbies.LobbyServiceException lobby) continue;
                switch (lobby.Reason)
                {
                    case Unity.Services.Lobbies.LobbyExceptionReason.LobbyFull: return "La sala está llena.";
                    case Unity.Services.Lobbies.LobbyExceptionReason.LobbyLocked: return "Espera a que el grupo regrese al lobby.";
                    case Unity.Services.Lobbies.LobbyExceptionReason.LobbyNotFound: return "Código incorrecto o sala cerrada.";
                }
            }
            for (Exception e = problem; e != null; e = e.InnerException)
            {
                string detail = e.Message.ToLowerInvariant();
                if (detail.Contains("session code") && detail.Contains("invalid")) return "Código incorrecto. Revisa los caracteres.";
                if (detail.Contains("full")) return "La sala está llena.";
                if (detail.Contains("locked")) return "Espera a que el grupo regrese al lobby.";
                if (detail.Contains("timeout") || detail.Contains("timed out")) return "La conexión tardó demasiado. Intenta otra vez.";
                if (e is AuthenticationException) return "No se pudo autenticar al jugador. Revisa tu conexión.";
                if (e is SessionException session)
                {
                    switch (session.Error)
                    {
                        case SessionError.InvalidParameter:
                        case SessionError.InvalidSessionIdentifier:
                        case SessionError.SessionNotFound: return "Código incorrecto o sala cerrada.";
                        case SessionError.SessionDeleted:
                        case SessionError.AllocationNotFound: return "El host cerró la sala.";
                        case SessionError.SessionTypeAlreadyExists: return "Ya perteneces a una sala.";
                    }
                }
            }
            return "No se pudo abrir la partida. " + problem.GetBaseException().Message;
        }
    }
}
