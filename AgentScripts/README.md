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
