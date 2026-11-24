# Blockganizer

Organizador semanal para Windows 11. Arrastra cubos de actividades a cualquier día y vuelve a usar los mismos bloques cuantas veces quieras.

## Descargar

[Descarga la última versión para Windows 11 x64](https://github.com/TheSpiralDen/Blockganizer/releases/latest). Elige el instalador MSI o el ZIP con la versión portable. Para usar la versión portable, extrae el ZIP y abre `Portable/Blockganizer.exe`. El MSI no tiene firma digital.

## Qué puedes hacer

- Crear, editar y archivar actividades en tu biblioteca.
- Mover y ordenar tus cubos en el tablero semanal.
- Agregar notas, marcar prioridades, pausar temporizadores y llevar objetivos semanales.
- Usar el modo claro u oscuro y cambiar la frase del encabezado.

Blockganizer funciona sin conexión. Tus actividades se guardan en SQLite de forma local en `%LOCALAPPDATA%\Blockganizer\data\blockganizer.sqlite3`.

## Compilar

Con Windows 11 x64 y .NET Framework 4.8 o posterior, ejecuta este comando desde la raíz del repositorio.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

El resultado se genera en `dist`. Agrega `-AppOnly` para compilar únicamente el ejecutable.
