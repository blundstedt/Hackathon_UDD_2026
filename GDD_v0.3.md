# GDD — Proyecto sin título

**Versión:** 0.3  
**Estado:** Borrador en desarrollo — sujeto a cambios  
**Actualización:** 22 de noviembre de 2026  
**Cambio principal respecto de v0.2:** diez niveles con escenarios independientes, alternancia de mapas compactos y parkours, laberinto en nivel 9 y uso del ascensor existente como entrada/salida y sistema de desbloqueo.

---

## 1. Concepto general

Juego **3D, en primera persona y multijugador**, ambientado en un universo absurdo y humorístico, con un estilo inspirado en *Gamble with Friends*.

En una ciudad donde la **inteligencia artificial está a punto de dominar el mundo**, los jugadores deben superar diez desafíos conectados por el ascensor del edificio principal, avanzar en la progresión y llegar al nivel final donde, a futuro, se enfrentarán al jefe principal.

**Decisión nueva:** los niveles se llaman *pisos* por su progresión en el ascensor, pero **no están situados físicamente dentro de las plantas de un edificio**. Son diez mapas independientes, visualmente distintos, a los que el ascensor permite viajar. Muchos consisten en islas o estructuras suspendidas en el cielo.

La experiencia completa combinará **combate, exploración, desafíos de movimiento y progresión incremental**, con talentos, compra de armas y mejoras de equipamiento. Habrá chat de voz por proximidad. El ciclo de regreso al lobby después de cada nivel continúa siendo central.

**Alcance de la tarea actual:** diseñar y construir los entornos de los diez niveles, sus recorridos, ascensores y conexiones mínimas de finalización/desbloqueo. **No desarrollar enemigos, combate, jefe, IA, armas ni habilidades en esta etapa.**

## 2. Contexto e historia

Una cinemática inicial anunciará la inminente conquista del mundo por la inteligencia artificial. Los jugadores llegan a un edificio central de una pequeña ciudad, cuyo ascensor permite acceder a una sucesión de escenarios imposibles. A medida que progresan, los mapas pueden hacerse visualmente más extraños, peligrosos y fragmentados.

La misión global es superar los diez niveles y, cuando se implemente el contenido de combate, derrotar al jefe final en el nivel 10.

**Pendiente:** definir quiénes son los jugadores, origen y motivaciones de la amenaza de IA, por qué el ascensor conecta dimensiones/escenarios, contenido de la cinemática, identidad de enemigos y jefe final, y desenlace narrativo.

## 3. Mundo y estructura del mapa

### 3.1. Edificio principal y ascensor

- El edificio principal **pertenece al lobby** y es el punto de acceso a los diez desafíos.
- Sus diez destinos **no son pisos interiores literales**: cada destino carga una escena diferente.
- Se reutilizará el asset/prefab existente en `Assets/Assets descargados/UrbanLobby/Provisional/Prefabs/MainBuilding2.0.prefab` o el componente de ascensor incluido en él.
- **La lógica del ascensor se presume ya implementada:** Codex debe inspeccionarla y probarla en el Editor. Si funciona, debe conservarla y usar sus scripts, interacción, UI, transición y progreso sin reescribirlos. Solo se permiten ajustes mínimos ante fallos o capacidades faltantes comprobadas.
- El nivel 1 empieza desbloqueado; completar el nivel N permite acceder al N+1, hasta el 10. Un nivel previamente desbloqueado puede revisitarse.
- **Decisión de diseño de mapas:** en cada escena habrá un **ascensor de llegada** al comienzo y un **ascensor de salida** al final. Ambos conservan la misma apariencia, idealmente reutilizando el ascensor ya existente o una variante segura, sin copiar accidentalmente un edificio completo.
- Volver desde el ascensor de llegada al lobby **no** marca el nivel completado. Activar el ascensor de salida después de recorrer el mapa **sí** registra su finalización y devuelve al lobby.
- Si la implementación actual del ascensor ya tiene un flujo equivalente que funciona, se aprovechará. Se verificará cómo enlazar las nuevas escenas y puntos de entrada/salida sin duplicar sistemas.

### 3.2. Ciudad y lobby

El lobby será una pequeña zona urbana explorable alrededor del edificio.

- Se mostrará ciudad en el horizonte para dar impresión de mundo más grande.
- Habrá espacio exterior para interacción y exploración con un enfoque parcialmente sandbox.
- Aquí los jugadores podrán prepararse y mejorar sus personajes entre niveles.
- El recorrido habitual será ciudad → ascensor → nivel → ascensor de salida → ciudad.
- **No se debe reconstruir ni rediseñar el lobby en la tarea actual** salvo para vincular las escenas y opciones de niveles a la lógica existente.

### 3.3. Locales y establecimientos

- **Tienda de habilidades:** desarrollo y gestión de talentos del personaje.
- **Tienda de armas:** establecimiento en las alcantarillas donde se comprarán armas con la moneda del juego.
- **Local de personalización:** cambios de apariencia del personaje.

Diseño interno, economía e interacciones concretas siguen pendientes. No forman parte de la construcción actual de los diez escenarios.

### 3.4. Catálogo oficial de los diez niveles

Cada nivel tendrá **una escena propia**. Las dimensiones son rangos orientativos, sujetos al tamaño de los assets y al movimiento real del jugador; no son un requisito que justifique saltos imposibles o espacios desproporcionados.

| Nivel | Nombre de trabajo y tipo | Escenario e intención visual | Cómo se llega a la salida |
|---|---|---|---|
| **1** | **Plaza en las nubes** — mapa compacto | Pequeña isla flotante con parque urbano, fuente, bancos, vegetación y edificios periféricos; ambiente luminoso. ~40 × 40 m. | Recorrido corto y seguro de plaza a plaza, con puente visible. |
| **2** | **Azoteas ascendentes** — parkour | Tejados y terrazas urbanas suspendidas, saltos anchos y ritmo introductorio. ~15–20 m de ascenso. | Subir progresivamente por edificios y plataformas hasta el ascensor superior. |
| **3** | **Último bloqueo** — mapa compacto | Trozo de avenida flotante clausurada, barricadas, conos, vehículos policiales/militares y asfalto fracturado. ~45 × 35 m. | Rodear el bloqueo central hasta el extremo opuesto de la calle. |
| **4** | **Autopista quebrada** — parkour | Segmentos de carretera, losas, barreras y vehículos a alturas crecientes. ~20–25 m de ascenso. | Recorrido serpenteante de saltos entre tramos de carretera y rampas. |
| **5** | **Barrio fragmentado** — mapa compacto | Tres islotes urbanos conectados: comercio, zona verde y plaza final, con iluminación cálida. ~50 × 45 m. | Cruzar las islas por pasarelas y rodear elementos urbanos. |
| **6** | **Jardines suspendidos** — parkour | Pequeñas islas rocosas con árboles y vegetación, dispuestas en espiral ascendente. ~25–30 m de ascenso. | Alternar saltos naturales y terrazas/rampas hasta el jardín superior. |
| **7** | **El borde del mundo** — mapa compacto | Isla irregular con naturaleza a un lado, restos urbanos quebrados al otro y horizonte de islas remotas. ~45 × 45 m. | Explorar ambos sectores y llegar al extremo seguro del borde. |
| **8** | **Ascenso al apagón** — parkour | Torre abierta de restos urbanos, edificios rotos y plataformas, con luces frías y silueta vertical. ~30–40 m de ascenso. | Ascenso helicoidal con saltos laterales, descansos y plataforma final panorámica. |
| **9** | **Laberinto de la ciudad perdida** — laberinto | Pasillos entre muros, fachadas y barreras urbanas en una isla contenida. ~50–65 m por lado. | Resolver un laberinto diseñado a mano con cruces, callejones, bucles y hitos visuales. |
| **10** | **Núcleo del colapso** — mapa compacto/futura arena final | Gran explanada suspendida rodeada de arquitectura urbana deformada; apariencia monumental y amenaza de IA. ~55 × 55 m. | Atravesar la arena hasta el ascensor opuesto; el futuro jefe no se implementa ahora. |

**Regla obligatoria de distribución:** 1/3/5/7/10 son mapas pequeños de ambientación; 2/4/6/8 son parkours **aéreos y ascendentes**, todos distintos; 9 es exclusivamente un laberinto bien diseñado.

### 3.5. Directrices específicas por tipo

**Mapas compactos (1, 3, 5, 7, 10)**

- Deben ser lugares pequeños, densos, memorables y bien delimitados; preferencia por islas flotantes o sectores urbanos suspendidos.
- La identidad visual debe reconocerse de inmediato al salir del ascensor.
- El ascensor de salida se ubica al final de un recorrido corto, no pegado al de llegada.
- El nivel 10 deja una superficie central suficientemente libre para un futuro jefe, sin agregarlo todavía.

**Parkours ascendentes (2, 4, 6, 8)**

- La salida se encuentra físicamente **más alta** que la llegada.
- Cada recorrido combina un tipo de superficie y composición diferente: tejados, carretera, naturaleza, torre urbana.
- Saltos y rampas deben ser posibles con las capacidades **reales** del controlador FPS; no presuponer doble salto ni movimiento avanzado.
- Debe haber plataformas de descanso y checkpoints; caerse provoca reaparición segura sin completar el nivel.
- Evitar atajos que permitan saltarse medio recorrido o llegar al ascensor final desde el inicio.
- La orientación visual debe dejar claro qué sigue y cuánto ha ascendido el jugador.

**Laberinto (9)**

- Debe diseñarse a mano con un grafo de caminos verificable: ruta entre entrada y salida, varias decisiones reales, callejones y algunos bucles.
- La salida no debe verse desde el inicio ni alcanzarse atravesando muros, saltándolos o caminando por techos.
- Incorporar referencias visuales que permitan orientarse sin eliminar el desafío.
- Sin enemigos ni parkour obligatorio en esta fase.

### 3.6. Assets, límites y calidad

El equipo cuenta con una base de recursos urbanos, naturales, mobiliario, carreteras, vehículos, skyboxes y props documentados en `ASSETS_CREDITS.md`:

- Low Poly Town Starter MegaKit y SimplePoly City para fachadas/edificios.
- Low Poly Environment - Nature Free para vegetación/naturaleza.
- AllSky Free para variaciones del cielo.
- Low Poly Park Pack para bancos, farolas, plaza, cercas y parque.
- Low poly Road Pack para barreras, conos, calles y detalles urbanos.
- Low Poly Free Vehicles Pack para vehículos estacionados.
- Low Poly Living Room Pack, Plates/Bowls/Mugs y Dangrek para ambientación/props/tipografía cuando correspondan.

**Estos packs son referencias de assets; Codex debe inspeccionar lo que efectivamente está importado**. No se puede asumir que exista un prefab particular solo por aparecer su pack en créditos. Priorizar reutilización, colisiones correctas, iluminación coherente, escenas livianas y composición visual cuidada. No descargar assets externos sin aprobación.

## 4. Bucle principal de juego

1. **Introducción:** cinemática presenta la amenaza de IA (pendiente de implementación).
2. **Lobby / Ciudad:** jugadores exploran, se preparan, gestionan talentos, compran armas y se personalizan (sistemas futuros donde aún no existan).
3. **Ascensor:** el jugador selecciona un nivel desbloqueado y viaja a su escena.
4. **Llegada:** aparece junto al ascensor de entrada del mapa, en suelo seguro.
5. **Desafío del mapa:** recorre la pequeña arena, sube un parkour o encuentra la salida del laberinto; más adelante podrán existir enemigos/objetivos de combate.
6. **Salida:** activa el ascensor final. Se registra el nivel como completado, se desbloquea el siguiente y se regresa al lobby.
7. **Mejoras:** tras volver al lobby, el jugador se prepara antes de elegir el próximo nivel.
8. **Progresión:** se repite hasta el 10. En el diseño completo, el último incluirá el jefe final; durante la tarea de mapas se completa llegando al ascensor de salida.

**Nota:** el jugador también puede regresar voluntariamente desde el ascensor de llegada, **sin** completar ni desbloquear el nivel siguiente. Completar niveles anteriores de nuevo no debe borrar progresión. La persistencia del avance se verificará en la lógica actual del ascensor antes de desarrollar algo nuevo.

**Pendiente:** regla de reintento tras fallar, interacción narrativa, condiciones futuras de cierre tras combate, sistema de progreso multijugador y qué ocurre después del jefe final.

## 5. Combate (visión futura, sin implementar en esta etapa)

El combate será **absurdo, chistoso y animado**, con tres categorías de armas:

- Cuerpo a cuerpo (*melee*).
- A distancia (*rango*).
- Magia.

Deberá relacionarse con talentos, clases y estilos de juego. **No implementar armas, enemigos, hitboxes, proyectiles, daños, jefes, spawns de enemigos ni IA dentro de la tarea de creación de mapas.** Reservar espacio para incorporarlos después.

**Pendiente:** armas específicas, efectos, clases, enemigos, mecánicas del jefe y balance de combate.

## 6. Progresión, talentos y economía (visión futura)

### 6.1. Talentos por nivel

Los personajes podrán subir de nivel. Existirán distintas clases, cada una con árbol de talentos ramificado, para especializar estilos de juego. Gestión desde el lobby.

**Pendiente:** clases, experiencia, reglas de desbloqueo de talentos y cambio de clase.

### 6.2. Compra de armas

Existirá una moneda o recurso (por definir), que permitirá adquirir armas en la tienda de las alcantarillas.

**Pendiente:** obtención de moneda, precios, disponibilidad de armas y restricciones por clase.

### 6.3. Mejoras de equipamiento

Se conseguirán mejoras mediante mecánicas/actividades distintas de la compra directa.

**Pendiente:** fuentes de mejoras, equipos afectados, efectos y conservación entre partidas.

### 6.4. Progreso de mapas (definido)

La secuencia 1→10 y el desbloqueo del siguiente nivel tras completar el anterior forman parte del alcance actual. Se debe usar primero el progreso ya implementado en el asset del ascensor, si funciona; de no funcionar, corregir estrictamente lo necesario. El guardado local no sustituye una futura autoridad de sesión/servidor para multijugador.

## 7. Multijugador y comunicación

La experiencia será multijugador en primera persona, con **chat de voz por proximidad** para favorecer la interacción social durante exploración y combate.

**Pendiente:** número máximo de jugadores, cooperación y reglas de grupo, sincronización de cambios de escena, autoridad del progreso y detalles de proximidad del chat.

La tarea actual **no incluye** desarrollar networking ni chat. Las escenas deben ser compatibles con una integración futura, evitando duplicar jugadores o managers persistentes al cargar niveles.

## 8. Dirección artística y tono

- **Formato:** 3D, cámara en primera persona.
- **Referencia de tono:** *Gamble with Friends*; absurdo, humorístico, desenvuelto.
- **Dirección visual:** low-poly estilizado, colores claros, objetos y escenarios coherentes con los assets disponibles.
- **Escenarios:** lobby urbano más diez escenas diferenciadas, muchas de ellas aéreas o flotantes.
- **Variación:** cielos, iluminaciones, paletas, siluetas, proporciones y landmarks distintivos por nivel.
- **Temática narrativa:** un mundo que parece fragmentarse al avanzar hacia la amenaza IA; no hace falta mostrar robots ni tecnología nueva en los escenarios.
- **Espacios extra:** locales, alcantarillas, parques, carreteras y exteriores explorables.

Se espera que las escenas resulten **visualmente terminadas** dentro de un prototipo low-poly: no grayboxes genéricos, decoraciones aleatorias ni escenarios vacíos sobredimensionados.

**Pendiente:** personajes, enemigos, música, audio, VFX específicos y guías definitivas de interfaz.

## 9. Alcance actual y criterios de aceptación

Esta tarea construye **entorno y navegación**, no los sistemas de combate.

**Entregables requeridos a Codex:**

1. Diez escenas `.unity` guardadas y nombradas, una por nivel, configuradas para cargar desde el proyecto.
2. En cada una, ascensor de llegada, spawn seguro, ascensor de salida, ruta transitables y colisiones consistentes.
3. Conexión con el ascensor **ya implementado**, tras probarlo. Reutilizar scripts y UI si funcionan; crear solo adaptaciones imprescindibles si no.
4. Desbloqueo secuencial 1→10 y regreso al lobby al completar, con retorno anticipado sin completar.
5. Parkours verticales 2/4/6/8 distintos, completables con el controlador real y protegidos contra caídas irrecuperables.
6. Laberinto 9 realmente resoluble, con ruta comprobada y sin atajos por colliders.
7. Cinco mapas pequeños variados, delimitados, trabajados visualmente y sin necesidad de combatir para recorrerlos.
8. Validación en Unity de apertura, navegación, transición, iluminación, referencias y errores de consola.
9. Informe de implementación con assets usados, ubicación de escenas, resultados de pruebas y cualquier limitación.

**No son entregables de esta tarea:** enemigos, boss, combate, armas, loot, talentos, economía, cinemática, voz, nuevos sistemas de networking ni funciones sandbox no relacionadas con los mapas.

## 10. Decisiones tomadas y pendientes

| Tema | Estado en v0.3 |
|---|---|
| Nombre del juego | Sin definir |
| Amenaza de la IA e introducción | Idea general definida; narrativa detallada pendiente |
| Ascensor en edificio del lobby | Asset existente identificado; lógica por probar/reutilizar |
| Diez niveles | **Definidos**: escenas independientes, no pisos interiores literales |
| Distribución de tipos | **Definida**: compactos 1/3/5/7/10; parkours 2/4/6/8; laberinto 9 |
| Ambientación de cada nivel | **Propuesta de diseño aprobada para implementar**, adaptable a assets reales |
| Llegada y salida | **Definido**: ascensor de inicio + ascensor final en cada mapa |
| Desbloqueo progresivo | **Definido**: completar N desbloquea N+1, con revisitables |
| Regreso al lobby | **Definido**: al completar y opcionalmente sin completar |
| Parkours ascendentes | **Definido**: 4 recorridos distintos, checkpoints y caídas seguras |
| Laberinto | **Definido**: construido a mano, rutas y colisiones válidas |
| Ciudad explorable | Idea general definida; detalles sandbox pendientes |
| Tienda de armas/habilidades y personalización | Concepto definido; mecánicas pendientes |
| Clases y talentos | Concepto definido; clases concretas pendientes |
| Economía y mejoras | Concepto definido; reglas pendientes |
| Armas, enemigos, combate, jefe | Visión futura; **fuera del alcance actual** |
| Número máximo de jugadores | Pendiente |
| Sincronización multijugador de niveles | Pendiente |
| Qué sucede tras derrotar al jefe | Pendiente |

## 11. Uso del GDD con IA/Codex

Este documento es la **fuente principal de diseño**. Para crear las escenas, acompañarlo del archivo `PROMPT_CODEX_ESCENAS_10_NIVELES.md`, que contiene el encargo de implementación y validación detallada.

1. Respetar decisiones confirmadas y no presentar los pendientes como reglas ya diseñadas.
2. Inspeccionar el Unity project real y assets importados antes de construir.
3. **Probar el ascensor existente y reutilizar su lógica si funciona.** Evitar gestores y scripts duplicados.
4. Construir las diez escenas y sus rutas, con el mínimo soporte de transición y progreso necesario.
5. No extender el alcance a combate ni networking por iniciativa propia.
6. Actualizar GDD y reporte si las restricciones reales del proyecto obligan a cambiar una parte del diseño.

