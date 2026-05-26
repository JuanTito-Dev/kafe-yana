# KafeYana API — Reembolso de Venta

**POST** `/api/Venta/reembolso/{Id}`

> Requiere caja abierta. Roles permitidos: `Admin`, `Cajero`, `Mesero`.

---

## Parámetros de ruta

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `int` | ID de la venta a reembolsar |

---

## Body (JSON)

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `TipoPago` | `int` | ✅ | Tipo de pago con el que se devuelve el dinero |
| `Monto` | `decimal` | ✅ | Monto a devolver (> 0, no puede superar el total de la venta) |
| `Motivo` | `string` | ✅ | Razón del reembolso |

---

## Enum `TipoPago`

| Valor numérico | Nombre |
|----------------|--------|
| `0` | `Efectivo` |
| `1` | `Tarjeta` |
| `2` | `Qr` |

---

## Ejemplo de request

```json
{
  "TipoPago": 0,
  "Monto": 25.50,
  "Motivo": "Producto en mal estado"
}
```

---

## Respuestas

```json
// 200 OK
{
  "message": "Reembolso procesado correctamente",
  "Codigo": "V-20260517-001"
}

// 400 — Tipo de pago inválido
{
  "message": "Tipo de pago inválido. Los valores permitidos son: Efectivo, Tarjeta, Qr"
}

// 400 — Venta ya reembolsada
{
  "message": "Esta venta ya fue reembolsada"
}

// 400 — Monto excede el total de la venta
{
  "message": "El monto a reembolsar no puede ser mayor al total de la venta"
}

// 400 — No hay caja abierta
{
  "message": "No hay una caja abierta"
}

// 404 — Venta no encontrada
{
  "message": "Venta no encontrada"
}
```

---

## Comportamiento en caja

Según el `TipoPago` enviado, se descuenta del total correspondiente en la caja activa:

| TipoPago | Campo afectado en Caja |
|----------|------------------------|
| `Efectivo` | `TotalEfectivo -= Monto` |
| `Tarjeta` | `TotalTarjeta -= Monto` |
| `Qr` | `TotalQr -= Monto` |

Se registra un movimiento de tipo **Egreso** con categoría `Reembolso` en `CajaMovimiento`.

La venta **no se modifica** en sus detalles ni totales — solo cambia su estado a `"Reembolsado"`.