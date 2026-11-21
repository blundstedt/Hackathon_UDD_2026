# Guía para Codex

## Contexto
- Este proyecto es un videojuego 3D cooperativo de supervivencia en un desierto fantástico, desarrollado con Unity 6 y C#.
- Mantén el código modular, legible y fácil de mantener; explica las decisiones técnicas importantes con claridad para alguien que está aprendiendo Unity.

## Cambios en el proyecto
- No modifiques escenas, prefabs, interfaces compartidas ni configuraciones globales fuera del alcance explícito de la tarea sin aprobación.
- No cambies paquetes ni dependencias sin autorización.
- Conserva los archivos `.meta` y sus referencias al añadir, mover o eliminar assets.
- Considera el futuro multijugador en los sistemas compartidos: define claramente quién posee y actualiza el estado, y evita dependencias innecesarias de estado global.
- Usa las skills oficiales de Unity cuando correspondan. Comprueba qué herramientas están disponibles y no asumas que una skill implica herramientas ejecutables.

## Git y colaboración
- Trabaja en una rama específica para cada tarea; no hagas cambios directos en `main`.
- Evita solaparte con el trabajo de las otras tres personas: limita cada cambio a los archivos necesarios para la tarea.
- No crees commits salvo que la tarea lo solicite.

## Verificación y entrega
- Revisa los cambios y comunica qué archivos modificaste.
- Indica qué pruebas o verificaciones realizaste y cuáles no pudiste ejecutar.
