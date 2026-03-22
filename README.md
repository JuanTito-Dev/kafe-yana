# KafeYana API

API REST FULL desarrollada en **.NET 9** para el sistema de **Kafe-Yana**.

## 🛠️ Tecnologías

- **.NET 9** — Web API
- **Entity Framework Core** — ORM
- **ASP.NET Identity** — Gestión de usuarios y roles
- **PostgreSQL** — Base de datos
- **JWT + HttpOnly Cookies** — Autenticación segura

## 🌐 Base URL
```
https://kafeyanaapi20260321224446-bqdjh9acame8gydt.centralus-01.azurewebsites.net/api
```

## 📚 Documentación

| Módulo | Descripción |
|--------|-------------|
| [🔐 Autenticación](/AuthEnpoints.md) | Registro, Login y Refresh Token |

## ⚠️ Importante para el Frontend

- Los tokens **no se retornan en el body** de la respuesta
- Los tokens se manejan automáticamente mediante **cookies HttpOnly**
- No es necesario almacenar tokens en `localStorage` ni `sessionStorage`
- Cuando recibas un `401 Unauthorized` llama automáticamente al endpoint `/RefreshToken`
- Todas las peticiones deben incluir `credentials: 'include'` para enviar las cookies

### Ejemplo con Fetch
```javascript
const response = await fetch('https://tu-app.azurewebsites.net/api/auth/login', {
  method: 'POST',
  credentials: 'include', // ← importante para las cookies
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});
```

## 🚧 Estado del Proyecto

En desarrollo activo.