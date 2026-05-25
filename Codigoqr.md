# API — Configuración de imagen QR (`/api/Qr`)

Hay **solo un QR** configurado para todo el sistema.

En las respuestas que incluyen dirección del archivo la propiedad siempre es **`Url`** (PascalCase en el JSON de esta API).

---

## 1. Obtener URL actual

**GET** `/api/Qr`

- **Autenticación:** ninguna (público).
- **Content-Type respuesta:** `application/json`.

**Respuesta cuando hay QR configurado:**

```json
{
  "Url": "https://tu-dominio-publico/Qr/abc123-qr.png"
}
```

**Respuesta cuando no hay QR:**

```json
{
  "Url": ""
}
```

**Ejemplo con `fetch`:**

```typescript
const res = await fetch(`${API_URL}/api/Qr`);
const data = await res.json();
const urlQr: string = data.Url;
```

**Ejemplo con `axios`:**

```typescript
const { data } = await axios.get<{ Url: string }>(`${API_URL}/api/Qr`);
const urlQr = data.Url;
```

---

## 2. Crear QR

**POST** `/api/Qr`

- **Autenticación:** Bearer JWT, rol **Admin**.
- **Request:** `multipart/form-data`.
- **Campo obligatorio del archivo:** `Imagen`.

**Formatos de imagen permitidos:**

- Extensiones: `.jpg`, `.jpeg`, `.png`, `.webp`
- MIME: `image/jpeg`, `image/png`, `image/webp`

Si **ya existe** un QR en base de datos, la API devuelve **400** indicando que deben usar **Actualizar**.

**Respuesta 200:**

```json
{
  "Url": "https://tu-dominio-publico/Qr/abc123-qr.png"
}
```

**Ejemplo envío:**

```typescript
const form = new FormData();
form.append("Imagen", file);

await axios.post<{ Url: string }>(`${API_URL}/api/Qr`, form, {
  headers: {
    Authorization: `Bearer ${accessToken}`,
    // Content-Type con boundary lo pone el cliente al enviar FormData
  },
});
```

**Errores frecuentes:**

- **400** — archivo faltante, formato no permitido, o QR ya existe.
- **401 / 403** — sin token o sin rol Admin.

---

## 3. Actualizar QR

**PUT** `/api/Qr`

- **Autenticación:** Bearer JWT, rol **Admin**.
- **Request:** `multipart/form-data`.
- **Campo obligatorio:** `Imagen`.

Elimina la imagen anterior en almacenamiento (R2), sube la nueva bajo carpeta **`Qr/`** y actualiza la URL en BD.

Si **no hay** registro previo → **400** (primero debe usarse crear).

**Respuesta 200:**

```json
{
  "Url": "https://tu-dominio-publico/Qr/nuevoNombre-qr.webp"
}
```

**Ejemplo:**

```typescript
const form = new FormData();
form.append("Imagen", file);

await axios.put<{ Url: string }>(`${API_URL}/api/Qr`, form, {
  headers: { Authorization: `Bearer ${accessToken}` },
});
```

---

## 4. Eliminar QR

**DELETE** `/api/Qr/eliminar`

- **Autenticación:** Bearer JWT, rol **Admin**.
- **Body:** ninguno.

Elimina el archivo en almacenamiento y borra el registro en BD. Si no hay QR → **400**.

**Respuesta 200:**

```json
{
  "message": "Código QR eliminado correctamente."
}
```

**Ejemplo:**

```typescript
await axios.delete(`${API_URL}/api/Qr/eliminar`, {
  headers: { Authorization: `Bearer ${accessToken}` },
});
```

---

## Tabla resumen

| Acción       | Método | Ruta               | Rol     | Respuesta principal        |
|-------------|--------|--------------------|---------|----------------------------|
| Consultar    | GET    | `/api/Qr`           | Público | `{ "Url": "..." }`          |
| Subir nueva  | POST   | `/api/Qr`           | Admin   | `{ "Url": "..." }`          |
| Reemplazar   | PUT    | `/api/Qr`           | Admin   | `{ "Url": "..." }`          |
| Eliminar     | DELETE | `/api/Qr/eliminar` | Admin   | `{ "message": "..." }`      |

---

## Notas para el front

1. Tras **GET**, `Url` puede ser **cadena vacía** si nadie cargó QR aún.
2. El nombre del campo multipart debe ser exactamente **`Imagen`** en POST y PUT.
3. Mismo esquema de **Bearer token** que en otros endpoints solo Admin.