# 🛍️ Query: productos

Endpoint GraphQL para obtener productos con filtros y paginación.

---

## 🔐 Autorización

- Requiere usuario autenticado
- Usa cookies HttpOnly (JWT)
- En frontend usar: `credentials: "include"`

---

## ⚙️ Parámetros

| Parámetro | Tipo   | Requerido | Descripción |
|----------|--------|----------|------------|
| tipo     | String | ❌ | Filtra por tipo (`Comprado`, `Elaborado`, `Combos`) |
| categoria| String | ❌ | Filtra por categoría |
| texto    | String | ❌ | Búsqueda parcial (nombre o descripción) |
| first    | Int    | ❌ | Cantidad de registros |
| after    | String | ❌ | Cursor para paginación |

---

## 📥 Ejemplos de uso

---

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
      stock
      costo
    }
  }
}

query {
  productos(categoria: "Bebidas") {
    nodes {
      id
      nombre
      categoriaNombre
    }
  }
}

query {
  productos(tipo: "Elaborado", texto: "latte") {
    nodes {
      id
      nombre
      recetaName
    }
  }
}