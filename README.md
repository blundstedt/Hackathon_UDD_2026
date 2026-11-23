# Hackathon UDD 2026 · La torre

Juego cooperativo en primera persona para Windows. Puedes jugar en solitario o compartir la campaña con hasta tres invitados.

## Jugar en Windows

1. [Descarga el juego para Windows de 64 bits (ZIP)](https://github.com/blundstedt/Hackathon_UDD_2026/releases/download/funcional/Torre-funcional-Windows-x64.zip).
2. Haz clic derecho en el ZIP y elige **Extraer todo**.
3. Abre la carpeta extraída y haz doble clic en **Torre.exe**. Mantén `Torre_Data`, `UnityPlayer.dll` y las demás dependencias junto al ejecutable.

En el equipo donde se compiló esta versión también puedes abrir `Builds\Campana-ee92b75\Torre.exe` desde la carpeta del proyecto.

En el menú principal, elige **Hostear Partida → Modo · Sin conexión** para jugar solo. Para jugar con amigos, el anfitrión elige **Modo · En línea**, entra al lobby y pulsa **Esc → Copiar**. Los invitados abren la misma versión del juego, eligen **Unirse Partida** e introducen el código.

Para viajar, entra en la cabina, apunta al botón del piso y pulsa **E**. En cooperativo, todo el grupo debe estar dentro. En el Piso 9, el botón **0** del ascensor de salida se enciende cuando termina la defensa. Consulta la [guía de la campaña cooperativa](NETWORKING.md) para más detalles.

## Ejecutar el proyecto desde Unity

1. Instala Git LFS y Unity Hub. En una terminal, descarga el proyecto y sus archivos de arte y audio.

   ```powershell
   git lfs install
   git clone https://github.com/blundstedt/Hackathon_UDD_2026.git
   cd Hackathon_UDD_2026
   git lfs pull
   ```

2. En Unity Hub, añade la carpeta que contiene `Assets`, `Packages` y `ProjectSettings`. Para esta versión, usa Unity **6000.6.10f1**. La versión del editor para cada revisión figura en [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt).
3. Espera a que Unity importe los recursos. Abre `Assets/Scenes/MenuPrincipal.unity` y pulsa **Play** para empezar en el menú.

## Ejecutar una versión posterior

Abre la [última versión publicada en GitHub](https://github.com/blundstedt/Hackathon_UDD_2026/releases/latest) y descarga el ZIP de Windows de la sección **Assets**. Extráelo en **una carpeta nueva** y abre el ejecutable de esa carpeta. Para cooperativo, todos los participantes deben abrir esa misma compilación.

Si los cambios están en GitHub y quieres generar el ejecutable tú mismo:

1. Desde una copia limpia del proyecto, actualiza el código y los recursos.

   ```powershell
   git switch main
   git pull --ff-only origin main
   git lfs pull
   git rev-parse --short HEAD
   ```

2. Abre la revisión con la versión de Unity indicada en `ProjectVersion.txt`. En **File → Build Profiles**, selecciona **Windows** para **Intel 64-bit (x64)**. Comprueba que la lista de escenas habilitadas empiece por `MenuPrincipal` e incluya el lobby y los pisos de esa versión.
3. Pulsa **Build** y elige una carpeta nueva, por ejemplo `Builds\Campana-<commit>\`, sustituyendo `<commit>` por el identificador que imprimió Git. Abre el `.exe` generado o usa **Build and Run** para iniciarlo directamente.
4. Para compartirla, comprime el ejecutable con su carpeta `*_Data`, las DLL y las demás dependencias que dejó Unity. Puedes excluir la carpeta de respaldo cuyo nombre termina en `_BackUpThisFolder_ButDontShipItWithYourGame`.
