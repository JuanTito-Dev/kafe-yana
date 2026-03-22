## 🔐 Autenticación

Base URL: `https://kafeyanaapi20260321224446-bqdjh9acame8gydt.centralus-01.azurewebsites.net/api/auth`

---

## 📝 Registro de Usuario

**`POST`** `/Registro`

Crea un nuevo usuario con rol **Admin** por defecto.

### Body (JSON)

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `nombre` | `string` | ✅ | Nombre del usuario |
| `apellido` | `string` | ✅ | Apellido del usuario |
| `email` | `string` | ✅ | Email válido y único |
| `password` | `string` | ✅ | Mínimo 5 caracteres |
| `numeroPhone` | `string` | ✅ | Número de teléfono |

### Ejemplo de Request
```json
{
  "nombre": "Juan",
  "apellido": "Perez",
  "email": "juan@gmail.com",
  "password": "12345",
  "numeroPhone": "591123456"
}
```

### Respuestas

| Código | Descripción |
|--------|-------------|
| `200 OK` | Usuario registrado correctamente |
| `400 Bad Request` | Datos inválidos o email ya registrado |

### Ejemplo de Response `200 OK`
```json
{
  "message": "Usuario Registrado"
}
```

### Ejemplo de Response `400 Bad Request`
```json
{
  "email": ["El email ya está registrado."]
}
```

## 🔑 Login de Usuario

**`POST`** `/Login`

Autentica al usuario y genera un **Access Token** y **Refresh Token** almacenados en cookies HttpOnly.

### Body (JSON)

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `email` | `string` | ✅ | Email registrado |
| `password` | `string` | ✅ | Contraseña del usuario |

### Ejemplo de Request
```json
{
  "email": "admin@gmail.com",
  "password": "Admin123#"
}
```

### Respuestas

| Código | Descripción |
|--------|-------------|
| `200 OK` | Login exitoso, cookies generadas |
| `400 Bad Request` | Datos inválidos |
| `401 Unauthorized` | Email o contraseña incorrectos |

### Ejemplo de Response `200 OK`
```json
{
  "message": "Usuario Encontrado"
}
```

### ⚠️ Cookies generadas automáticamente

| Cookie | Descripción | Duración |
|--------|-------------|----------|
| `ACCESS_TOKEN` | JWT para autenticar requests | 5 minutos |
| `REFRESH_TOKEN` | Token para renovar el Access Token | 7 días |

> Las cookies son **HttpOnly**, **Secure** y **SameSite=Strict**, no son accesibles desde JavaScript.

## 🔄 Refresh Token

**`POST`** `/RefreshToken`

Renueva el **Access Token** expirado usando el **Refresh Token** almacenado en la cookie. Este endpoint debe llamarse automáticamente cuando el Access Token expire.

### ¿Cuándo usarlo?

Cuando el servidor devuelva `401 Unauthorized` en cualquier request protegido, el frontend debe llamar a este endpoint automáticamente para obtener un nuevo Access Token sin necesidad de que el usuario vuelva a hacer login.

### Request

No requiere Body. Lee el `REFRESH_TOKEN` directamente de las cookies.

### Respuestas

| Código | Descripción |
|--------|-------------|
| `200 OK` | Tokens renovados correctamente |
| `400 Bad Request` | Refresh Token no encontrado en cookies |
| `401 Unauthorized` | Refresh Token inválido o expirado |

### Ejemplo de Response `200 OK`
```json
{
  "message": "Token revocado"
}
```

### ⚠️ Cookies renovadas automáticamente

| Cookie | Descripción | Duración |
|--------|-------------|----------|
| `ACCESS_TOKEN` | Nuevo JWT generado | 5 minutos |
| `REFRESH_TOKEN` | Nuevo Refresh Token generado | 7 días |

### ⏱️ Duración de los Tokens

| Token | Duración | ¿Por qué? |
|-------|----------|-----------|
| `ACCESS_TOKEN` | 5 minutos | Corta duración por seguridad, si es robado expira rápido |
| `REFRESH_TOKEN` | 7 días | Permite renovar el Access Token sin re-login |
