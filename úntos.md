# Documentación — Sistema de Puntos KafeYana

---

## REST Endpoints — `PuntosController`

Base URL: `/api/puntos`

---

### Regla Base

#### `GET /api/puntos/config/reglabase`
Ver la configuración actual de la regla base.

- **Rol requerido:** Admin

**Respuesta `200`:**
```json
{
  "id": 1,
  "cantidad": 10.00,
  "activo": true
}
```

---

#### `POST /api/puntos/config/reglabase`
Crear la regla base. Solo se hace una vez.

- **Rol requerido:** Admin

**Body:**
```json
{
  "Cantidad": 10,
  "Activo": true
}
```

**Respuesta `201`:**
```json
{ "message": "Regla base creada", "id": 1 }
```

---

#### `PUT /api/puntos/config/reglabase`
Actualizar cantidad o estado de la regla base.

- **Rol requerido:** Admin

**Body:**
```json
{
  "Cantidad": 10,
  "Activo": true
}
```

**Respuesta `200`:**
```json
{ "message": "Regla base actualizada" }
```

---

### Aceleradores

#### `GET /api/puntos/config/aceleradores`
Ver todos los aceleradores con su configuración actual.

- **Rol requerido:** Admin

**Respuesta `200`:**
```json
[
  {
    "id": 1,
    "tipo": "Combo",
    "tipoAplicacion": "Suma",
    "cantidad": 2,
    "umbralMonto": null,
    "horaInicio": null,
    "horaFin": null,
    "activo": false
  },
  {
    "id": 2,
    "tipo": "CompraAlta",
    "tipoAplicacion": "Multiplicador",
    "cantidad": 2,
    "umbralMonto": 100,
    "horaInicio": null,
    "horaFin": null,
    "activo": false
  },
  {
    "id": 3,
    "tipo": "CompraMediana",
    "tipoAplicacion": "Suma",
    "cantidad": 2,
    "umbralMonto": 70,
    "horaInicio": null,
    "horaFin": null,
    "activo": false
  },
  {
    "id": 4,
    "tipo": "Cumpleanos",
    "tipoAplicacion": "Multiplicador",
    "cantidad": 2,
    "umbralMonto": null,
    "horaInicio": null,
    "horaFin": null,
    "activo": false
  },
  {
    "id": 5,
    "tipo": "HoraValle",
    "tipoAplicacion": "Suma",
    "cantidad": 2,
    "umbralMonto": null,
    "horaInicio": "14:00",
    "horaFin": "17:00",
    "activo": false
  }
]
```

---

#### `PUT /api/puntos/config/aceleradores/{id}`
Actualizar un acelerador por su Id (del 1 al 5).

- **Rol requerido:** Admin

**Body para aceleradores normales (id: 1, 2, 3, 4):**
```json
{
  "Cantidad": 3,
  "Activo": true
}
```

**Body para HoraValle (id: 5) — `HoraInicio` y `HoraFin` son obligatorios:**
```json
{
  "Cantidad": 4,
  "Activo": true,
  "HoraInicio": "16:00",
  "HoraFin": "19:00"
}
```

**Respuesta `200`:**
```json
{ "message": "Acelerador actualizado" }
```

---

### Ajuste Manual de Puntos

#### `POST /api/puntos/cliente/{clienteId}/ajuste`
Agregar puntos manualmente a un cliente con un motivo registrado.

- **Rol requerido:** Admin

**Body:**
```json
{
  "Cantidad": 50,
  "Motivo": "Compensación por error en venta VTA-2026-001"
}
```

**Respuesta `200`:**
```json
{
  "message": "50 puntos agregados al cliente",
  "PuntosActuales": 120
}
```

---

## GraphQL Queries

### Ver puntos de un cliente

Los puntos están directamente en el campo `puntos` de la entidad `Cliente`.

**Todos los clientes con sus puntos:**
```graphql
query {
  clientes {
    nodes {
      id
      nombre
      celular
      puntos
    }
  }
}
```

**Un cliente específico:**
```graphql
query {
  clientes(where: { id: { eq: 1 } }) {
    nodes {
      id
      nombre
      puntos
    }
  }
}
```

---

### Ver historial de puntos

**Todos los registros con paginación:**
```graphql
query {
  historialPuntos {
    totalCount
    nodes {
      id
      codigoVenta
      puntosBase
      puntosFinales
      desglose
      fecha
    }
  }
}
```

**Filtrar por cliente:**
```graphql
query {
  historialPuntos(where: { idCliente: { eq: 1 } }) {
    totalCount
    nodes {
      id
      codigoVenta
      puntosBase
      puntosFinales
      desglose
      fecha
    }
  }
}
```

**Filtrar por cliente y ordenar del más reciente:**
```graphql
query {
  historialPuntos(
    where: { idCliente: { eq: 1 } }
    order: { fecha: DESC }
  ) {
    totalCount
    nodes {
      id
      codigoVenta
      puntosBase
      puntosFinales
      desglose
      fecha
    }
  }
}
```

---

## Lógica de cálculo de puntos

Los puntos se calculan automáticamente al procesar una venta (`POST /api/venta/cobrar`).

### Reglas

| Paso | Descripción |
|------|-------------|
| 1 | `PuntosBase = floor(TotalVenta / ReglaBase.Cantidad)` |
| 2 | Si `PuntosBase = 0` o la regla está inactiva → no se aplica nada |
| 3 | Cada **multiplicador** activo calcula `PuntosBase × Cantidad` de forma independiente y se suman entre sí |
| 4 | Cada **sumador** activo agrega su `Cantidad` al resultado final |

### Aceleradores

| Id | Tipo | Aplicación | Condición |
|----|------|------------|-----------|
| 1 | Combo | Suma | Si la venta contiene algún combo |
| 2 | CompraAlta | Multiplicador | Si el total supera el `UmbralMonto` (default 100 bs) |
| 3 | CompraMediana | Suma | Si el total supera el `UmbralMonto` (default 70 bs) |
| 4 | Cumpleaños | Multiplicador | Si hoy es el cumpleaños del cliente |
| 5 | HoraValle | Suma | Si la hora de la compra está dentro del rango configurado |

> Los aceleradores solo se aplican si `PuntosBase > 0` y si `Activo = true`.

### Ejemplo

```
TotalVenta   = 115 bs
ReglaBase    = 10 bs/punto
PuntosBase   = floor(115 / 10) = 11

CompraAlta activo (x2)  → 11 × 2 = 22
Cumpleaños activo (x3)  → 11 × 3 = 33
Multiplicadores total   = 22 + 33 = 55

HoraValle activo (+4)   → +4
Sumas total             = +4

PuntosFinales = 55 + 4 = 59
Desglose = "CompraAlta:x2=22 | Cumpleanos:x3=33 | HoraValle:+4"
```

---

## Flujo completo de configuración inicial

```
1. POST /api/puntos/config/reglabase
   Body: { "Cantidad": 10, "Activo": true }

2. PUT /api/puntos/config/aceleradores/2
   Body: { "Cantidad": 2, "Activo": true }   ← activar CompraAlta

3. PUT /api/puntos/config/aceleradores/4
   Body: { "Cantidad": 3, "Activo": true }   ← activar Cumpleaños

4. PUT /api/puntos/config/aceleradores/5
   Body: { "Cantidad": 4, "Activo": true, "HoraInicio": "14:00", "HoraFin": "17:00" }

5. POST /api/venta/cobrar                    ← los puntos se calculan automáticamente

6. GraphQL: clientes(where: id eq X)         ← ver puntos actualizados del cliente

7. GraphQL: historialPuntos(where: idCliente eq X, order: fecha DESC)
```