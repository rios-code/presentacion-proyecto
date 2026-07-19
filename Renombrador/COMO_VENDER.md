# Cómo poner Renombrador Pro a la venta (paso a paso)

Esta guía es la parte que **no es código**: cómo pasar de "tengo el programa" a "recibo dinero".
Está pensada para tu **primera venta**, sin complicarte. Después optimizás.

---

## El modelo de venta (importante entenderlo)

- El programa se reparte **gratis** y funciona en **modo prueba** (hasta 10 archivos por vez).
- Lo que **vendés es la CLAVE de licencia**, que desbloquea el programa sin límites.

Así la gente prueba antes de pagar, y vos solo entregás una clave cuando alguien compra.

---

## Paso 1 — Creá TU propio par de claves (obligatorio)

El programa viene con una clave pública de ejemplo (la mía). Para vender **tenés que usar la tuya**,
si no, no vas a poder generar claves válidas.

1. Abrí Git Bash en la carpeta del proyecto.
2. Generá tu par:
   ```bash
   cd Herramientas/Generador
   dotnet run
   ```
   La primera vez crea dos archivos: `privada.pem` (SECRETA, guardala bien) y `publica.pem`.
3. Abrí `publica.pem` (con el Bloc de notas) y copiá **todo** su contenido.
4. Pegalo en `Renombrador/GestorLicencia.cs`, reemplazando el texto de la constante
   `CLAVE_PUBLICA` (entre las líneas `-----BEGIN PUBLIC KEY-----` y `-----END PUBLIC KEY-----`).

> ⚠️ **Nunca** compartas ni subas `privada.pem`. Es la que te da el control. Si alguien la tiene,
> puede fabricar claves gratis.

---

## Paso 2 — Generá el `.exe` para distribuir

```bash
cd Renombrador
bash publicar.sh
```

Queda en: `bin/Release/net10.0/win-x64/publish/Renombrador.exe`
Ese único archivo es lo que descargan tus clientes (les funciona en modo prueba).

---

## Paso 3 — Creá tu cuenta de venta (Gumroad)

1. Entrá a **gumroad.com** y creá una cuenta (gratis).
2. Configurá cómo cobrar (te pide datos para pagarte; Gumroad acepta tarjetas de todo el mundo).
3. Creá un producto nuevo:
   - **Nombre**: Renombrador Pro — Licencia
   - **Precio**: el que elijas (ej: US$7). Podés cambiarlo cuando quieras.
   - **Tipo**: producto digital.

---

## Paso 4 — Qué subís a Gumroad

Tenés dos opciones para la descarga del programa:

- **Simple (recomendada para empezar):** subí el `Renombrador.exe` como archivo del producto.
  Quien compra, lo descarga; quien no, igual lo podés ofrecer gratis desde tu landing.
- O dejá el `.exe` gratis en tu landing y que Gumroad **solo venda la clave**.

---

## Paso 5 — Cómo entregás la licencia (la clave)

**Al principio, hacelo manual.** Es lo más simple y te sirve para validar que la gente compra:

1. Alguien compra → Gumroad te avisa por email (con el nombre/email del comprador).
2. Abrís el Generador (`cd Herramientas/Generador && dotnet run`), opción **1**, y generás una
   clave para ese cliente.
3. Le enviás la clave por email (o por el mensaje post-compra de Gumroad).

> Cuando tengas varias ventas por semana, se puede automatizar. Pero para arrancar, manual está
> perfecto: no pierdas tiempo automatizando algo que todavía nadie compró.

---

## Paso 6 — Conectá tu landing page

1. En `Renombrador/landing/index.html`, reemplazá:
   - `href="#descargar"` → el link de descarga de tu `.exe`.
   - `href="#comprar"` → el link de tu producto en Gumroad.
   - El precio de ejemplo (`US$7`) por el tuyo.
2. Subí la landing gratis a **Netlify** (arrastrás la carpeta `landing/`) o **GitHub Pages**.
   Te dan una URL para compartir.

---

## Paso 7 — Conseguí los primeros compradores

Esto es lo más difícil y es 100% tuyo. Ideas para empezar sin gastar:
- Grupos de Facebook / foros / Reddit donde la gente hable de organizar fotos o archivos.
- Mostrar un antes/después en video corto (TikTok, Instagram, YouTube Shorts).
- Ofrecerlo gratis a 5–10 personas a cambio de que te digan qué mejorarían (feedback + testimonios).

---

## Realismo (para que no te frustres)

- Hay renombradores **gratis** (PowerRename de Microsoft, Bulk Rename Utility). Vender uno genérico
  es difícil. Si ves que no engancha, el mayor valor de este proyecto es como **demostración de que
  sabés hacer software completo** (sirve para conseguir trabajo o clientes de desarrollo).
- La primera venta es la más difícil. Enfocate en **un tipo de usuario** (ej: gente que ordena
  muchas fotos) y hablales a ellos.

Éxitos. Ya tenés la parte difícil hecha: el producto existe y funciona.
