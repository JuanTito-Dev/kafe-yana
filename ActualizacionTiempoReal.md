# Cambio en el evento `StockActualizado` — SignalR

## ¿Qué cambió?

El payload **tiene la misma estructura de siempre**, pero ahora incluye **todos los productos afectados indirectamente**, no solo los que estaban en la ronda que se acaba de procesar.

## ¿Por qué?

**Antes:** si el mesero A agregaba un "Cafe Filtrado", el evento solo traía el stock de ese producto.

**Ahora:** el backend detecta todos los insumos que se consumieron (ej. cafe molido, leche) y busca **todos los productos que usan esos mismos insumos**, aunque no hayan estado en esa ronda. También detecta si un comprado vendido pertenece a un combo y reporta el nuevo disponible del combo.

---

## Estructura del payload (sin cambios)

```ts
interface StockActualizadoPayload {
  comprados: CompradoStockItem[];
  elaborados: ElaboradoStockItem[];
  combos: ComboStockItem[];
}

interface CompradoStockItem {
  id: number;    // Id del Producto
  stock: number; // Stock actual post-venta
}

interface ElaboradoStockItem {
  id: number;                      // Id del Producto
  stock: number;                   // Stock físico (solo si producible = true, si no es 0)
  cantidadProducible: number | null; // Porciones que se pueden hacer ahora (solo si producible = false con receta)
}

interface ComboStockItem {
  id: number;                 // Id del Producto del combo
  cantidadProducible: number; // Cuántos combos se pueden armar con el stock actual de sus componentes
}
```

---

## Lo que el front DEBE hacer

Cuando llegue el evento `StockActualizado`, actualizar el stock de **TODOS** los productos que vengan en el payload, sin importar si esos productos están en la ronda actual o no.

```ts
connection.on("StockActualizado", (payload: StockActualizadoPayload) => {

  payload.comprados.forEach(item => {
    actualizarStockProducto(item.id, item.stock);
  });

  payload.elaborados.forEach(item => {
    if (item.cantidadProducible !== null) {
      actualizarProducibleProducto(item.id, item.cantidadProducible);
    } else {
      actualizarStockProducto(item.id, item.stock);
    }
  });

  payload.combos.forEach(item => {
    actualizarProducibleCombo(item.id, item.cantidadProducible);
  });

});
```

---

## Problema que esto resuelve

Antes el front solo actualizaba los productos que "conocía" de la transacción local. Los otros dispositivos conectados nunca recibían la actualización de productos relacionados (ej. dos cafés que comparten el mismo insumo). Ahora el backend manda todo lo afectado, el front solo tiene que aplicar todo lo que llega.