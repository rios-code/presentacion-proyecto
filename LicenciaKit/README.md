# LicenciaKit

**Protegé y vendé tu software con licencias de activación firmadas digitalmente.**

LicenciaKit es una librería en C# (.NET) que te deja generar y validar **claves de licencia**
como las de un juego o programa comercial (`XXXXX-XXXXX-XXXXX-...`), usando **firma digital
ECDSA P-256**. Sin dependencias externas y con validación **offline**.

---

## ¿Para qué sirve?

Si desarrollás un programa y querés cobrar por él, necesitás una forma de que **solo quien
pagó** pueda usarlo. LicenciaKit te da exactamente eso:

1. El cliente te paga.
2. Vos le generás una **clave** con tu herramienta (tenés la clave privada).
3. El cliente pega la clave en tu app; tu app la valida con la **clave pública** embebida.
4. Si la clave es legítima y no venció, la app funciona. Si es falsa o alterada, la rechaza.

**Nadie puede fabricar claves válidas sin tu clave privada.** Cambiar un solo carácter de una
clave la invalida.

---

## Los dos roles

| Rol | Clase | Tiene | Puede |
|-----|-------|-------|-------|
| **Vendedor** (vos) | `GeneradorLicencias` | clave **privada** | generar licencias |
| **Cliente** (tu app) | `ValidadorLicencias` | clave **pública** | solo validar |

La clave privada **nunca** se distribuye. Solo se embebe la pública.

---

## Uso rapido

### 1. Crear tu par de claves (una sola vez)

```csharp
var (privada, publica) = ClavesLicencia.CrearPar();
// Guardá 'privada' en un lugar seguro (solo vos).
// 'publica' se embebe en tu app.
```

### 2. Generar una licencia (lado vendedor)

```csharp
var generador = new GeneradorLicencias(privada);
Licencia lic = generador.Generar("Mi Programa", "cliente@mail.com", diasValidez: 0); // 0 = permanente
Console.WriteLine(lic.Clave);   // -> se la das al cliente
```

### 3. Validar en tu app (lado cliente)

```csharp
var validador = new ValidadorLicencias(publica);   // 'publica' embebida como constante
ResultadoValidacion r = validador.Validar(claveDelUsuario);

if (r.EsValida)
    Console.WriteLine($"Bienvenido, {r.Cliente}");
else
    Console.WriteLine($"Licencia invalida: {r.Mensaje}");  // Estado: Vencida, FirmaInvalida, etc.
```

Revocación opcional (offline): pasale una lista de claves anuladas al validador:

```csharp
var validador = new ValidadorLicencias(publica, clavesRevocadas: new[] { "XXXXX-..." });
```

---

## Herramientas incluidas

- **`Herramientas/Generador`** — app de consola para el vendedor: crea el par de claves y
  emite licencias, guardando un registro en `licencias_emitidas.csv`.
- **`Herramientas/AppProtegidaDemo`** — ejemplo de una app de cliente que pide una clave al
  iniciar y se bloquea si no es válida. Copiá esta idea a tu propio programa.

Probarlas:

```bash
cd Herramientas/Generador && dotnet run          # crea claves + emite (anota la clave)
cd ../AppProtegidaDemo && dotnet run             # pegá la clave (necesita publica.pem al lado)
```

---

## Cómo se gana dinero con esto (honesto)

LicenciaKit **no genera dinero por sí solo**: es la herramienta que te permite **cobrar por tu
propio software**. El camino real:

1. Desarrollás un programa/juego **tuyo** (o con derechos para venderlo).
2. Integrás LicenciaKit para que requiera una licencia.
3. Lo vendés (tu web, redes, marketplaces) y cobrás con una pasarela (Stripe, Mercado Pago, etc.).
4. Cuando alguien paga, le generás y enviás su clave.

> Importante: solo podés vender software **propio o con licencia para venderlo**. No sirve para
> distribuir programas de terceros sin permiso.

---

## Seguridad

- Firma **ECDSA P-256** (curva elíptica): fuerte y con claves cortas.
- La clave privada nunca sale del vendedor; la app solo lleva la pública.
- Validación **offline y autosuficiente**: la clave contiene sus datos + la firma.

**Nunca** subas `privada.pem` a un repositorio ni la incluyas en la app que distribuís.
