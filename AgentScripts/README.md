El juego se inicia en **MenuPrincipal**. Para probar el multijugador, los slots y el menú de pausa, consulta [Partidas cooperativas](../NETWORKING.md). Las instrucciones que siguen documentan las pruebas del prototipo local anterior a esa integración.

# Movimiento FPS básico

Abrir `Assets/Scenes/MovementTest.unity` y pulsar Play. Si Game no tiene el foco, hacer clic en su vista.

- WASD: caminar. Mouse: mirar. Espacio: saltar desde el suelo.
- Shift izquierdo: correr mientras se mantiene pulsado.
- Escape: liberar cursor y suspender entrada de movimiento/mirada. Clic izquierdo: capturarlo.
- La gravedad continúa con el cursor libre. Perder foco libera el cursor; volver requiere clic.

## Prefab e Inspector

Arrastrar `Assets/Prefabs/Player.prefab` a cualquier mapa con suelo y colliders. Colocar la raíz a la altura del suelo y evitar otra cámara/AudioListener activos.

- PlayerMovement: Walk Speed (4.5 m/s), Sprint Speed (7.5 m/s), Jump Height (1.2 m), Gravity (20 m/s², magnitud positiva).
- FirstPersonLook: Mouse Sensitivity (0.1 grados por unidad de delta), Pitch Limit (85 grados), Camera Pivot.
- CharacterController: altura, radio, escalones y límite de pendiente.
- CameraPivot: altura de ojos (1.65 m). FirstPersonCamera: FOV (75 grados).
- PlayerInputReader: referencia al asset existente `InputSystem_Actions`. Se clona por instancia y usa sus bindings Keyboard&Mouse; no modifica el asset compartido.

La raíz contiene el controlador y gira solamente en horizontal. CameraPivot gira en vertical; Visual es la cápsula reemplazable, sin collider adicional. Su Renderer usa Shadows Only para evitar que tape la vista; al sustituir el modelo hay que configurar su visibilidad para primera persona.

Esta es una implementación local. En el futuro solo el jugador local debe tener entrada, cámara y AudioListener activos; no hay networking, combate ni dependencias de progresión.

## Herramientas de autoría y verificación

`BuildMovementTest.cs` crea los assets mediante las APIs del Editor y se niega a sobrescribirlos o cerrar escenas con cambios sin guardar. Se conserva fuera de Assets para no incluir herramientas de autoría en el juego.

`VerifyMovement.cs` es un script de evaluación temporal para ejecutar únicamente en Play Mode con MovementTest y la ventana Game enfocada. Usa dispositivos virtuales y los elimina al terminar. No forma parte de las assemblies del juego. Ejecución con Unity CLI:

```powershell
unity command script eval --file AgentScripts/VerifyMovement.cs --caller plugin --skill unity-pipeline
```

Verificado en Unity 6000.6.10f1: compilación, avance de frames, suelo, WASD, velocidades de caminar/correr, salto, ausencia de salto aéreo, aterrizaje, gravedad, yaw, límite de pitch, colisión con pared y liberar/recapturar cursor. Pendiente: prueba manual con teclado y mouse físicos para valorar sensibilidad y sensación de movimiento.

No se modifican SampleScene, Build Settings, paquetes ni ajustes globales. Los assets nuevos incluyen sus archivos .meta. No se creó ningún commit; el punto inicial de referencia es 4ae387b.

## Ascensor y progresión de la partida

Para probar el recorrido, inicia Play en `UrbanLobby_new`, entra en la cabina y apunta al botón 1. Pulsa E para viajar. En `Piso1`, usa E sobre el pedestal y regresa con el botón 0. Entrega la llave a **Recepcionista - Tiza con peluca** y usa el botón 2 para llegar al atrio de Paraíso Celestial. En el templo, apunta al diamante y pulsa E para cobrar dinero y EXP. Reúne al grupo en la cabina de salida y usa el botón 0. El piso 3 queda habilitado tras completar el piso 2. Hasta integrar la nueva escena, el botón 3 informa de que el piso está pendiente.

### Añadir un piso desde el Inspector

1. En `Assets/Progression`, usa **Create → Hackaton → Progresión → Piso**. Asigna un número único, un nombre visible y el nombre exacto de su escena. El 0 identifica el lobby. Deja `Initially Available` desmarcado en los pisos que abre la secretaria.
2. Crea el objeto con **Create → Hackaton → Progresión → Objeto de desbloqueo**. Edita su nombre e icono. En el piso anterior, configura `Experience`, `Money` y `Reward Item`. En el piso nuevo, configura `Prerequisite` con el piso anterior y `Required Item` con el mismo asset. Para un piso final puedes dejar su `Reward Item` vacío.
3. Abre `Assets/Progression/FloorSession.prefab` y añade la definición al arreglo `Floors`. Si aún no hay escena, deja vacío `Scene Name`. Así podrás verificar la entrega sin iniciar una carga.
4. En la nueva escena, coloca `Assets/Progression/Ascensor.prefab`, un punto con `FloorSpawn` y el prefab habitual en `Default Avatar`. Comprueba que el punto tenga suelo libre. Añade la escena a **File → Build Profiles → Scene List**. Coloca `FloorObjective` en el mismo objeto que su collider para usar E, o llama a `FloorSession.CompleteCurrent(participante)` desde el objetivo real.
5. El panel original tiene botones para los números 0–9. Para ampliar físicamente el panel, añade el dígito al arreglo `Digits` en el índice del piso y coloca un `FloorButton` con el mismo destino, su panel y un collider. Actualiza el edificio del lobby y, si deseas mostrar ese indicador en los niveles, el prefab de la cabina.

### Pantalla de carga y tipografía

Abre `Assets/Progression/FloorSession.prefab` y edita **Interfaz de viaje → Pantalla de carga**. El objeto contiene el fondo, el texto y el `CanvasGroup` que cubre la escena. `Fade Seconds` está en `FloorTravelScreen`, en **Interfaz de viaje**. Puedes cambiar el diseño conservando las referencias `Curtain` y `Prompt`. Los textos usan la fuente uGUI `Assets/Fonts/Dangrek-Regular (1).ttf`.

### Responsabilidades y duración del progreso

| Pieza | Responsabilidad |
|---|---|
| `FloorData` y `UnlockItem` | Configuración estática de pisos, requisitos, recompensas e identidad de la llave. |
| `FloorSession` | Autoridad local persistente, registro de jugadores, carga asíncrona, reparto de premios y restauración de estadísticas y equipamiento por identidad. |
| `FloorProgress` | Estados bloqueado, disponible y completado, inventario del equipo, consumo de llaves y evento de cambio. |
| `TowerFloorPanel` y `FloorButton` | Iluminación, selección del destino, zona de embarque y espera hasta que todos estén dentro. |
| `FloorPlayer` y `FloorSpawn` | Identidad del avatar, alta y baja en la sesión, interacción desde la cámara y aparición con el equipamiento elegido. |
| `FloorInteraction`, `FloorSecretary` y `FloorObjective` | Contrato de interacción con E, entrega de la llave y finalización del nivel. |
| `FloorTravelScreen` | Mensajes de interacción, conteo de espera y fundido de la pantalla. |

La EXP y el dinero se acreditan mediante `PlayerStats`. Cada participante conserva sus premios por ID. Si se desconecta durante el nivel y su equipo lo completa, el premio queda pendiente para esa identidad durante la partida. La llave pertenece al equipo y se consume una sola vez. Para reiniciar el progreso, detén Play e inicia otra partida, o cierra y vuelve a abrir el juego.

La cabina compara los avatares registrados con su zona en el momento de partir. Si quien solicitó el viaje sale o se desconecta, cancela la solicitud. La integración online futura debe asignar una identidad estable antes de activar cada avatar, mantener la lista de participantes desde el servidor, validar allí las interacciones y coordinar la carga y la réplica del progreso en los clientes.

### Pruebas en Play Mode

Ejecuta cada entrada en una partida nueva iniciada en `UrbanLobby_new`. Para la prueba con teclado virtual, da foco a la vista Game y captura el cursor.

```powershell
unity command script run --caller plugin --skill unity-pipeline --file AgentScripts/VerifyFloorTravel.cs --entry VerifyFloorTravel.All
```

`All` comprueba el recorrido, la entrega única, los rayos de interacción, el equipamiento, la espera con cuatro avatares locales y la desconexión durante el regreso. Para recorrer el mismo flujo con la tecla E del Input System, ejecuta la segunda entrada.

```powershell
unity command script run --caller plugin --skill unity-pipeline --file AgentScripts/VerifyFloorTravel.cs --entry VerifyFloorTravel.KeyboardFlow
```
