## 🛍️ Query: GetProductos

Obtiene una lista de productos con soporte de filtrado por tipo y paginación.

⚠️ Este endpoint está protegido y solo puede ser usado por usuarios con rol **Admin**.

---

## 🔐 Autorización

- Requiere autenticación
- Rol requerido: `Admin`

---

## ⚙️ Parámetros

| Nombre | Tipo   | Requerido | Descripción |
|--------|--------|----------|------------|
| Tipo | string | ❌ | Filtro por tipo de producto (aunque el nombre del parámetro puede inducir a error) |

---

## 🎯 Comportamiento del parámetro `nombre`

Este parámetro controla el **tipo de productos a devolver**:

| Valor enviado | Resultado |
|--------------|----------|
| `"Comprado"` | Devuelve solo productos comprados |
| `"Elaborado"` | Devuelve solo productos elaborados |
| `"Combos"` | Devuelve solo productos tipo combo |
| `null` o no enviado | Devuelve **todos los productos** |

📌 **Nota importante para frontend:**  
Aunque el parámetro se llama `nombre`, **realmente funciona como un filtro por tipo**.

---

## ⚡ Funcionalidades automáticas

Este endpoint utiliza:

- `UsePaging` → paginación automática
- `UseFiltering` → filtros adicionales (opcional)
- `UseSorting` → ordenamiento

---

## 📥 Ejemplos de uso

### 🔹 Obtener todos los productos

```graphql
query {
  productos {
    nodes {
      id
      nombre
      tipo
    }
  }
}

query {
  productos(tipo: "Comprado") {
    nodes {
      id
      nombre
      tipo
      stock
      costo
    }
  }
}