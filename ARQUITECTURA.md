# Arquitectura del proyecto

El proyecto está dividido en tres partes que comparten la misma lógica interna.

```
Nucleo/            -> Librería compartida (toda la lógica interna)
proyecto final/    -> App de consola: EMISOR de licencias + herramienta
AppEscritorio/     -> App de escritorio (Avalonia): CLIENTE con login y biblioteca
```

## Nucleo (librería compartida)

Contiene la lógica que usan las dos apps, para no duplicar nada:

| Clase | Rol |
|-------|-----|
| `EmisorServicio` | Rol EMISOR: tiene la clave **privada**, genera licencias firmadas (ECDSA P-256). |
| `LicenciaServicio` | Rol CLIENTE: solo la clave **pública**; valida y revoca, no puede generar. |
| `Licencia`, `FormatoClave` | Entidad de licencia y utilidades de formato de la clave. |
| `Base32`, `ResultadoValidacion` | Codificación de la clave y resultado de validar. |
| `JuegoLocal`, `BibliotecaServicio` | Biblioteca de juegos por usuario (canje e instalación). |
| `CuentaServicio` | Registro y login con contraseñas hasheadas (PBKDF2 + salt). |
| `SteamServicio`, `JuegoSteam` | Cliente de la API oficial de Steam (solo lectura). |
| `BaseDatos`, `Migracion` | Base de datos **SQLite** y migración desde los `.txt` viejos. |
| `Rutas` | Carpeta de datos **compartida** por las dos apps. |

## Almacenamiento (SQLite)

Los datos (cuentas, licencias y bibliotecas) se guardan en una **base de datos SQLite**
(`mitienda.db`), con tablas `cuentas`, `licencias` y `biblioteca`. El par de claves de firma
sigue en archivos `.pem` (son claves criptográficas, no datos).

Todo vive en una carpeta común del usuario (`%APPDATA%/MiTienda` en Windows,
`~/.config/MiTienda` en Linux), **fuera del repositorio**. Gracias a esto:

- El **emisor** (consola) y el **cliente** (escritorio) usan **la misma base y las mismas claves**.
- Una licencia generada por el emisor **se valida y se canjea en el cliente**.
- Los datos sensibles (clave privada, cuentas) nunca se suben a git.
- Si existían datos viejos en `.txt`, se **migran automáticamente** a SQLite la primera vez.

## Separación de roles (emisor / cliente)

- El **emisor** (`EmisorServicio`) tiene la **clave privada** (`clave_privada.pem`) y es el único
  que puede **generar** licencias. En la app está detrás de la sección ADMIN.
- El **cliente** (`LicenciaServicio`) solo necesita la **clave pública** (`clave_publica.pem`)
  para **validar**. No puede generar licencias.
- Para distribuir el cliente de verdad, se entrega **solo `clave_publica.pem`**; la privada
  nunca sale del emisor. Un cliente sin la clave privada jamás puede fabricar una licencia válida.

## Portadas de juegos

Las portadas se generan (degradado + formas + inicial). Si querés usar **imágenes propias**,
poné un archivo `covers/<nombre-del-juego>.png` (o `.jpg`) dentro de la carpeta de datos
(`~/.config/MiTienda/covers/` o `%APPDATA%\MiTienda\covers\`) y la app la usará automáticamente.

## Flujo completo (emisor → cliente)

1. **Emisor** (consola): genera una licencia firmada para un juego y un cliente.
2. El cliente recibe la clave.
3. **Cliente** (escritorio): inicia sesión → sección *Canjear* → pega la clave.
4. El Núcleo verifica **firma, vencimiento y revocación**. Si es válida, el juego aparece
   en su **biblioteca** y puede instalarlo.
5. Una clave adulterada, vencida o revocada **no desbloquea nada**.

## Seguridad

- **Licencias:** firma digital ECDSA P-256. Solo la clave privada genera licencias válidas;
  cualquiera valida con la pública. Cambiar un carácter invalida la firma.
- **Contraseñas:** PBKDF2 con salt aleatorio por cuenta. Nunca se guardan en texto plano.

## Cómo correr

```bash
# Emisor / herramienta de consola
cd "proyecto final" && dotnet run

# Cliente de escritorio
cd AppEscritorio && dotnet run
```

Requiere el SDK de .NET 10.
