# Renombrador Pro

**App de escritorio para renombrar muchos archivos a la vez.** Es un producto propio,
listo para vender, protegido con licencias (LicenciaKit).

## Qué hace

Renombra archivos en lote con reglas y **vista previa en vivo** antes de aplicar:

- Buscar y reemplazar texto en los nombres.
- Agregar prefijo y/o sufijo.
- Numeración automática (con cantidad de dígitos y número inicial).
- Pasar a MAYÚSCULAS o minúsculas.
- **Filtrar por extensión** (ej: solo `jpg, png`).
- **Deshacer**: revierte el último renombrado con un clic (seguridad total).
- Nunca sobrescribe archivos: avisa si hay nombres repetidos.

Ejemplo: `IMG_0021.jpg` → `viaje_IMG_0021_001.jpg`

## Cómo se activa (protección con licencia)

Al abrir por primera vez pide una **clave de licencia**. Solo con una clave válida se puede usar.
La clave se valida con la clave pública embebida (firma ECDSA); una clave falsa o alterada se rechaza.

## Cómo se ejecuta

```bash
cd Renombrador
dotnet run
```

## Cómo lo convertís en TU producto para vender

1. **Generá tu par de claves**: corré `Herramientas/Generador` (crea `privada.pem` y `publica.pem`).
   Guardá `privada.pem` en un lugar seguro (¡nunca la compartas!).
2. **Pegá tu clave pública** en `GestorLicencia.cs`, en la constante `CLAVE_PUBLICA`
   (reemplazá la que viene de ejemplo).
3. **Generá el `.exe`**: ejecutá `bash publicar.sh` (o `publicar.bat` en Windows). Crea UN
   solo archivo (`bin/Release/net10.0/win-x64/publish/Renombrador.exe`) con todo incluido:
   tus clientes hacen doble clic, **no necesitan instalar .NET ni nada**.
4. **Vendé** la app (tu web, redes, marketplaces) y cobrá con Stripe, Mercado Pago, etc.
5. Cuando alguien paga, generás su clave con el `Generador` y se la enviás.

> Solo podés vender software propio o con derechos. Renombrador es 100% tuyo, así que estás en regla.

## Idea de precio

Herramientas simples de este tipo se venden como pago único (ej: 5–15 USD) o licencia por PC.
Empezá barato, sumá funciones (mover a subcarpetas, filtros por extensión, deshacer) y subí el precio.
