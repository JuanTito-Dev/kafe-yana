## 👤 Query: Me

Obtiene la información del usuario actualmente autenticado.

Este endpoint identifica al usuario mediante **cookies HttpOnly** que contienen:
- JWT (access token)
- Refresh token

---

## 🔐 Autenticación

- Requiere autenticación (`[Authorize]`)
- No usa headers manuales en frontend
- Usa **cookies HttpOnly automáticas**

### 📌 Importante

El navegador envía automáticamente las cookies en cada request si:

- `credentials: "include"` está habilitado
- El backend permite `credentials` en CORS

---

## ⚙️ Parámetros

❌ No recibe parámetros

El usuario se identifica automáticamente desde el token almacenado en cookies.

---

## 🧠 Funcionamiento interno

1. El backend lee las cookies (JWT)
2. Extrae los `claims`
3. Obtiene el `NameIdentifier` (ID del usuario)
4. Busca el usuario en base de datos
5. Retorna los datos

---

## ❌ Posibles errores

| Error | Descripción |
|------|------------|
| No autorizado | No hay cookies válidas |
| "info no encontrado" | No se pudo obtener el ID del token |
| "Usario no encontrado" | Usuario no existe en BD |

---

## 📥 Ejemplo de Query

```graphql
query {
  me {
    nombre
    apellido
    userName
    email
    celular
    estado
  }
}