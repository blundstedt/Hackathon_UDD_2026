# Partidas cooperativas

La primera escena es `Assets/Scenes/MenuPrincipal.unity`. En el Editor, `MenuStartScene` configura el mismo punto de entrada al pulsar Play. La portada usa la ilustración entregada por el equipo. Los controles son elementos uGUI editables y comparten el prefab `Assets/Multiplayer/UI/BotonNeon.prefab` y la fuente Dangrek.

## Abrir una partida

1. En **Hostear Partida**, deja **Modo · En línea** y crea o continúa uno de los tres slots. Con **Modo · Sin conexión**, el mismo slot se puede jugar localmente.
2. En el lobby, pulsa **Esc** y **Copiar**. Comparte el código actual con hasta tres amigos. Cada uno inicia la misma versión del juego y elige **Unirse Partida**.
3. Dentro del ascensor, apunta al botón del piso y pulsa **E**. La cabina cuenta a los jugadores conectados. El último en entrar habilita la salida si quien pidió el viaje sigue dentro.
4. Después del objetivo, vuelve en ascensor con el botón **0** y entrega la llave en recepción. Los pisos con escena son 1, 2, 3 y 9. El piso 10 sigue pendiente en el catálogo original.
5. Usa **Esc → Volver al Menú Principal** para salir. El host guarda al cerrar la sala. Continuar la partida crea una sesión y un código nuevos.

## Autoridad y persistencia

- `CampaignRoom` crea una sola instancia de `RedDePartida` al iniciar una conexión. El SDK inicia NGO mediante `CreateSessionAsync` con `WithRelayNetwork()` o `JoinSessionByCodeAsync`. Un código corresponde a una sesión activa. El host elimina la sesión con `IHostSession.DeleteAsync()`.
- `NetworkTiza` monta el prefab real de manos vacías, bastón, resortera o taza en cada máquina. El dueño controla movimiento y cámara. El `NetworkTransform` sincroniza posición y giro. Las copias remotas muestran el modelo en tercera persona. El host ejecuta la interacción solicitada, comprueba el alcance y selecciona el punto de aparición.
- `FloorProgress` conserva el catálogo del GDD y aplica una recompensa por piso. El registro de `FloorSession` asigna EXP, dinero, salud, nivel y equipamiento por identidad. La llave pertenece al grupo. La cuenta `local` identifica al dueño del slot tanto en línea como sin conexión. Los invitados conservan su identidad mediante la autenticación anónima de su instalación.
- Cada slot tiene un JSON propio en `Application.persistentDataPath/Partidas/partida-N.json`. `CampaignSlots` escribe un temporal en el mismo directorio, lo vacía al disco y reemplaza la versión anterior conservando una copia `.bak`. La interfaz pide confirmación para eliminar ambas copias del slot. Los ajustes personales utilizan PlayerPrefs de la máquina.
- Al continuar se vuelve al lobby con el progreso guardado. Un piso abandonado sin completar se puede volver a intentar. El sistema guarda el progreso de campaña y las cuentas de los participantes, no la posición momentánea de enemigos o proyectiles.
- En el Piso 9 el host dirige la persecución, la caída, las tres oleadas y el cofre. Los invitados reciben la posición, la animación y la vida de cada enemigo diez veces por segundo. El Corruptor conserva su identidad al volver como jefe. Cualquier participante puede activar la terminal y recoger el cofre para el grupo.
- La mesa de ping-pong conserva su partida individual contra la máquina. Su marcador es local y no concede dinero, EXP ni llaves.

## Versiones y APIs

`ProjectSettings/ProjectVersion.txt` contiene `m_EditorVersion: 6000.6.10f1`. `Packages/manifest.json` fija `"com.unity.services.multiplayer": "2.5.0"`, `"com.unity.netcode.gameobjects": "2.14.0"` y `"com.unity.multiplayer.playmode": "3.0.0"`. El archivo `Packages/packages-lock.json` resuelve Transport 6.6.0 y Authentication 3.9.0.

Las firmas de creación y unión están en `Runtime/Multiplayer/MultiplayerService.cs` dentro del SDK. El manejador NGO del paquete usa `NetworkManager.Singleton` y, en `NetworkManagerSession.cs`, llama a `NetworkManager.StartHost()`. En NGO, `NetworkTransform.AuthorityMode`, `SpawnAsPlayerObject` y `NetworkSceneManager.OnLoadEventCompleted` permiten integrar propiedad, aparición y viajes. La configuración del proyecto reutiliza su vínculo existente con Unity Cloud.

Referencias de Unity consultadas para sesiones, autenticación, códigos e integración NGO. [Skill histórica solicitada](https://github.com/Unity-Technologies/skills/blob/8d672b8c1b9deafbddec3268ede10569062780a0/skills/setup-multiplayer-services/SKILL.md), [integración de sesiones](https://docs.unity.com/en-us/mps-sdk/sessions), [autenticación](https://docs.unity.com/en-us/mps-sdk/init-ugs), [sesión con NGO](https://docs.unity.com/en-us/mps-sdk/tutorials/build-your-first-session).

## Pruebas reproducibles

El escenario `Assets/Multiplayer/Cooperativo.asset` contiene un Editor principal y un jugador virtual. `AgentScripts/VerifyCoop.cs` se ejecuta mediante `unity command script run --file AgentScripts/VerifyCoop.cs --entry VerifyCoop.Interface` y entradas como `Slots`, `Host` y `State`. Las pruebas de slots escriben en un directorio temporal exclusivo. Para llamar desde un jugador virtual, pasa la ruta absoluta del script y selecciona la instancia con `--instance 'Player 2'`.

### Resultados de la build de Windows

- Una misma sala privada de Unity reunió tres procesos en un PC mediante su código real. El host y un cliente usaron Multiplayer Play Mode. El tercer participante entró desde el ejecutable independiente. Se comprobó el movimiento remoto, la separación de los Tizas, la salida y reconexión de invitados y el regreso de los clientes al menú cuando cerró el host.
- El ascensor indicó que faltaba un participante y esperó al grupo. Se probaron los viajes al lobby y a los pisos 1, 2, 3 y 9 con sus escenas reales. El objetivo del piso 1 se activó dos veces y concedió una sola recompensa a cada participante. Durante una visita a un piso, el juego rechazó una incorporación y la permitió de nuevo en el lobby.
- El host reabrió el slot de prueba sin conexión y conservó los cuatro pisos completados, el piso 10 desbloqueado y sus estadísticas de nivel 2 con 135 de dinero y 170 de experiencia. Las pruebas independientes de archivo cubrieron dos slots, el respaldo de un JSON dañado y el borrado de un solo slot.
- Se verificaron ESC, el cursor y la copia del código en ambos jugadores de Play Mode. En Windows se verificaron el menú principal, el acceso por código, opciones persistentes tras reiniciar y la salida al escritorio.
- La comprobación del quinto jugador ejercitó la aprobación del servidor con cuatro plazas ocupadas. Quedan por probar cinco conexiones reales simultáneas y dos computadores en redes físicas distintas.
- La compilación x64 terminó con cero errores y dos advertencias. Unity señaló 13 mallas con precálculo de colisiones deshabilitado y emitió un aviso de vinculación de Services Core. El proyecto contiene su identificador de Unity Cloud y el ejecutable se autenticó y jugó a través de Relay.
