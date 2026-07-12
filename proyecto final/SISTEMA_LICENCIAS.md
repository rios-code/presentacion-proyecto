# Generador de Licencias — Steam (nivel fuerte)

Sistema de consola en C# que **genera y valida claves de activación** de juegos usando
**firma digital de curva elíptica (ECDSA P-256)**.

## Idea de seguridad

- Al iniciar por primera vez se crea un **par de claves**:
  - `clave_privada.pem` → sirve para **generar** licencias (la guarda el emisor, es secreta).
  - `clave_publica.pem` → sirve para **validar** licencias (puede repartirse con el juego).
- Cada clave lleva **dentro** los datos (juego, cliente, vencimiento) **+ su firma**.
- Sin la clave privada es **imposible fabricar** una clave que pase la validación, y si alguien
  altera un solo carácter, la firma deja de coincidir y se rechaza.
- La validación es **offline**: no necesita servidor ni base de datos.

## Estructura del código

| Archivo | Rol |
|---------|-----|
| `Licencia.cs` | Entidad: datos de una licencia y su estado (vigente/vencida/revocada). |
| `LicenciaServicio.cs` | Lógica fuerte: crea/carga las claves, firma, valida, revoca y persiste. |
| `Base32.cs` | Codifica los bytes firmados como texto legible (A–Z, 2–7). |
| `ResultadoValidacion.cs` | Resultado de validar: válida/ inválida + motivo + datos. |
| `SteamServicio.cs` | Cliente de la **API oficial de Steam** (solo lectura). |
| `program.cs` | Menú de consola. |

## Integración con Steam (API oficial de Valve)

Además de las licencias locales, el proyecto se conecta a la API **oficial** de Steam
para **leer** información real (nunca activa ni genera juegos):

- **Pública (sin credenciales):** info, precio y descripción de cualquier juego de la
  tienda por su AppID (ej: `220` = Half-Life 2).
- **Guardar biblioteca:** exporta tu biblioteca a `biblioteca_steam.csv` (AppID, nombre,
  horas jugadas), que se abre en Excel/LibreOffice. También queda fuera del repositorio.
- **Personal (con tu cuenta):** tu biblioteca de juegos y tu perfil. Requiere:
  - una **API key** gratuita: https://steamcommunity.com/dev/apikey
  - tu **SteamID64** (17 dígitos).

  Se ingresan al ejecutar y se guardan en `steam_config.txt`, **excluido del repositorio**.

## Menú

1. **Generar** nueva licencia (pide juego, cliente y días de validez; `0` = permanente).
2. **Validar** una licencia (verifica firma, vencimiento y revocación).
3. **Listar** licencias emitidas.
4. **Revocar** una licencia.
5. Salir.

## Cómo se forma la clave

```
[datos: juego | cliente | vencimiento]  --firma ECDSA-->  [firma]
        \_______________________________ + ____________________/
                              |
                    empaquetado en bytes
                              |
                     codificado en Base32
                              |
              XXXXX-XXXXX-XXXXX-...  (la clave final)
```

## Cómo ejecutar

```bash
cd "proyecto final"
dotnet run
```

> Nota: `clave_privada.pem`, `clave_publica.pem` y `licencias.txt` se generan al ejecutar
> y están excluidos del repositorio (la clave privada es secreta).
