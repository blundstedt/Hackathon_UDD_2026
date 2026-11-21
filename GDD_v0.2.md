# GDD — Proyecto sin título

**Versión:** 0.2  
**Estado:** Borrador en desarrollo — sujeto a cambios

---

## 1. Concepto general

Juego **3D, en primera persona y multijugador**, ambientado en un universo absurdo y humorístico, con un estilo inspirado en *Gamble with Friends*.

La historia se desarrolla en una ciudad donde la **inteligencia artificial está a punto de dominar el mundo**. Los jugadores deberán enfrentarse a enemigos controlados por IA, avanzando por los pisos de un edificio hasta alcanzar el último nivel y derrotar al jefe final.

El juego combinará **combate, exploración y progresión incremental**, permitiendo a los jugadores desarrollar sus personajes mediante talentos, compra de armas y mejoras de equipamiento.

Los jugadores podrán comunicarse mediante chat de voz por proximidad.

La experiencia se organizará alrededor de un ciclo de combate y mejoras, regresando al lobby después de cada nivel.

## 2. Contexto e historia

Al comenzar el juego, una **cinemática introductoria** anunciará que la inteligencia artificial está a punto de dominar el mundo.

Ante esta amenaza, los jugadores se dirigirán a un edificio donde deberán combatir contra distintos enemigos asociados a la IA.

El edificio estará dividido en pisos que representan los niveles del juego. El objetivo será avanzar hasta el último piso, donde se encuentra el jefe final.

**Pendiente:**
- Definir quiénes son los jugadores y por qué deciden enfrentar a las IA.
- Definir el origen de la amenaza.
- Determinar qué representa el edificio y por qué es importante.
- Definir a los enemigos y al jefe final.
- Desarrollar el contenido concreto de la cinemática.

## 3. Mundo y estructura del mapa

El mundo estará compuesto por un **edificio principal de combate** y una pequeña zona urbana explorable que funcionará como lobby.

### 3.1. Edificio principal

- El edificio estará dividido en diferentes pisos.
- Cada piso corresponderá a un nivel de combate.
- Los jugadores deberán superar los desafíos del piso para desbloquear el siguiente.
- La dificultad aumentará progresivamente.
- El último piso contendrá el enfrentamiento contra el jefe final.
- **Después de completar cada piso, los jugadores regresarán al lobby** antes de avanzar al siguiente.

### 3.2. Ciudad y lobby

El lobby estará ubicado en una pequeña zona de ciudad alrededor del edificio.

- El mapa explorable será relativamente pequeño.
- Se mostrará una ciudad más extensa en los alrededores para generar la sensación de un mundo más grande.
- Los jugadores podrán moverse por el exterior del edificio.
- El exterior incluirá elementos de exploración e interacción, con un enfoque parcialmente sandbox.
- En esta zona se encontrarán los establecimientos donde los jugadores podrán preparar y mejorar sus personajes.

### 3.3. Locales y establecimientos

**Tienda de habilidades**

Espacio donde los jugadores podrán gestionar o desarrollar las habilidades y talentos de sus personajes.

**Tienda de armas**

Establecimiento ubicado en las alcantarillas, donde los jugadores podrán comprar nuevas armas utilizando el recurso económico del juego.

**Local de personalización**

Lugar donde los jugadores podrán modificar la apariencia de sus personajes.

**Pendiente:**
- Definir la cantidad de pisos.
- Diseñar los desafíos y enemigos de cada nivel.
- Determinar las actividades y mecánicas sandbox disponibles en el exterior.
- Definir el diseño y funcionamiento de cada local.
- Determinar cómo se realiza la transición entre el lobby y los pisos.

## 4. Bucle principal de juego

El juego tendrá una estructura incremental basada en la superación de niveles y el desarrollo del personaje.

1. **Introducción:** una cinemática presenta la amenaza de la IA.
2. **Lobby / Ciudad:** los jugadores aparecen en la ciudad, donde pueden explorar, comprar armas, gestionar talentos y personalizarse.
3. **Ingreso al edificio:** los jugadores acceden al piso correspondiente a su progreso.
4. **Combate:** deben enfrentarse a enemigos y superar los desafíos del nivel.
5. **Finalización del piso:** al completarlo, reciben las recompensas correspondientes y desbloquean el siguiente.
6. **Regreso al lobby:** los jugadores regresan a la ciudad para mejorar sus personajes, adquirir equipamiento y prepararse.
7. **Siguiente piso:** cuando estén preparados, ingresan nuevamente al edificio para enfrentar el próximo nivel.
8. **Jefe final:** el ciclo continúa hasta alcanzar el último piso y enfrentarse al jefe final.

**Resumen del ciclo principal:**

Lobby → Piso N → Combate → Recompensas → Lobby → Mejoras → Piso N+1 → Repetir hasta el jefe final.

El regreso al lobby será una parte fundamental de la experiencia, permitiendo a los jugadores prepararse entre los diferentes niveles.

**Pendiente:** definir qué sucede después de derrotar al jefe final y cómo funciona la repetición del juego una vez completado el edificio.

## 5. Combate

El combate tendrá un estilo **absurdo, chistoso y animado**, coherente con la identidad del juego.

Existirán tres categorías de armas:

- **Cuerpo a cuerpo (melee).**
- **A distancia (rango).**
- **Magia.**

Las armas deberán mantener un enfoque humorístico tanto en su apariencia como en sus efectos y animaciones.

El combate también estará relacionado con el sistema de talentos, permitiendo desarrollar diferentes estilos de juego.

**Pendiente:**
- Definir las armas concretas de cada categoría.
- Establecer sus efectos y mecánicas.
- Diseñar los diferentes tipos de enemigos.
- Definir las mecánicas del jefe final.
- Determinar cómo se relacionan las armas y las clases.

## 6. Progresión, talentos y economía

El juego tendrá un sistema de progresión incremental compuesto por tres elementos principales.

### 6.1. Sistema de talentos por nivel

- Los jugadores podrán subir de nivel.
- Existirán diferentes clases de personaje.
- Cada clase contará con su propio **árbol de talentos ramificado**.
- Los talentos permitirán desarrollar distintas capacidades y estilos de juego.
- Los jugadores podrán gestionar sus mejoras desde el lobby.

**Pendiente:**
- Definir las clases.
- Diseñar los árboles de talentos.
- Determinar cómo se obtiene experiencia.
- Establecer cómo se desbloquean o adquieren talentos.
- Definir si las clases se pueden cambiar.

### 6.2. Sistema de compra de armas

Los jugadores podrán adquirir nuevas armas mediante un sistema económico.

- Existirá un recurso o moneda del juego, todavía sin definir.
- Las armas se podrán comprar en una tienda ubicada en las alcantarillas.
- La compra permitirá ampliar las opciones de combate de los jugadores.

**Pendiente:**
- Definir el recurso económico.
- Determinar cómo se obtiene.
- Establecer los precios y tipos de armas disponibles.
- Definir si todas las clases pueden utilizar todas las armas.

### 6.3. Mejoras de equipamiento

Además de los talentos y de la compra de armas, los jugadores podrán **obtener mejoras de equipamiento mediante mecánicas del juego**.

Este sistema será diferente de la compra directa de armas y permitirá fortalecer el equipamiento a través de la participación en distintas actividades.

**Pendiente:**
- Definir las mecánicas para conseguir mejoras.
- Determinar qué partes del equipamiento pueden mejorarse.
- Establecer los efectos de estas mejoras.
- Definir cuáles se conservan entre niveles o partidas.

## 7. Multijugador y comunicación

La experiencia será multijugador, con perspectiva en primera persona.

Los jugadores podrán comunicarse mediante **chat de voz por proximidad**, haciendo que la comunicación dependa de la distancia entre ellos.

Este sistema estará disponible para fomentar la interacción durante el combate y la exploración.

**Pendiente:**
- Definir la cantidad máxima de jugadores.
- Determinar las reglas de cooperación y las posibles interacciones entre jugadores.
- Definir los detalles de la comunicación por proximidad.

## 8. Dirección artística y tono

La identidad visual y sonora del juego tendrá un enfoque **absurdo, humorístico y desenfadado**.

- **Formato:** 3D.
- **Perspectiva:** primera persona.
- **Referencia visual:** *Gamble with Friends*.
- **Estilo de combate:** armas y efectos animados y cómicos.
- **Ambientación:** ciudad con un edificio principal de varios pisos.
- **Espacios adicionales:** locales comerciales, alcantarillas y exteriores explorables.
- **Enemigos:** relacionados con la temática de la inteligencia artificial.

Se buscará una identidad artística coherente entre personajes, enemigos, armas y escenarios.

**Pendiente:**
- Definir el aspecto concreto de los personajes.
- Establecer el estilo visual de los enemigos.
- Diseñar el edificio y los espacios urbanos.
- Determinar la música, los sonidos y los efectos visuales.

## 9. Decisiones pendientes

| Tema | Estado |
|---|---|
| Nombre del juego | Sin definir |
| Contexto de la IA | Idea general definida |
| Cinemática inicial | Concepto definido |
| Identidad de los jugadores | Sin definir |
| Edificio como escenario principal | Definido |
| Pisos como niveles | Definido |
| Regreso al lobby entre pisos | Definido |
| Ciudad explorable | Idea general definida |
| Locales de mejoras y compras | Concepto definido |
| Mecánicas sandbox | Sin definir |
| Clases de personaje | Sin definir |
| Sistema de talentos ramificado | Concepto definido |
| Recurso económico | Sin definir |
| Compra de armas | Concepto definido |
| Mejoras de equipamiento | Concepto definido |
| Armas específicas | Sin definir |
| Enemigos y jefe final | Sin definir |
| Cantidad de pisos | Sin definir |
| Número de jugadores | Sin definir |
| Sistema de progresión posterior al boss | Sin definir |

## 10. Cómo usar este GDD con IA

Este documento será la **fuente principal de referencia para el desarrollo del juego asistido por IA**.

- Respetar las decisiones confirmadas en este documento.
- No tratar los puntos pendientes como decisiones tomadas.
- Cuando una tarea requiera definir algo que continúa abierto, proponer opciones antes de incorporarlo al diseño base.
- Mantener el GDD actualizado conforme el equipo acuerde nuevas ideas.
- Distinguir entre la visión completa del juego y las funcionalidades que se implementarán en el prototipo de la hackatón.

**Alcance de esta versión:** describe la visión conceptual y las mecánicas principales del juego. No establece todavía el alcance definitivo del prototipo jugable ni los detalles técnicos de implementación.