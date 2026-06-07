// init-products.js
// Seeds sample products into the `inventory` database on first container startup
// Placed in /docker-entrypoint-initdb.d/ so the official Mongo image runs it automatically

try {
  // Switch to the inventory DB
  db = db.getSiblingDB('inventory');

  // Create collection if missing and ensure unique index on Sku
  if (!db.getCollectionNames().includes('products')) {
    db.createCollection('products');
  }
  db.products.createIndex({ Sku: 1 }, { unique: true });

  // Seed documents (matches ProductInventory model)
  const seed = [
    { Sku: "SKU-1", Name: "Widget A", TotalQuantity: 100, AvailableQuantity: 100, ReservedQuantity: 0 },
    { Sku: "SKU-2", Name: "Widget B", TotalQuantity: 50, AvailableQuantity: 50, ReservedQuantity: 0 },
    { Sku: "SKU-3", Name: "Gadget C", TotalQuantity: 200, AvailableQuantity: 200, ReservedQuantity: 0 },
    { Sku: "SKU-4", Name: "Gizmo D", TotalQuantity: 10, AvailableQuantity: 10, ReservedQuantity: 0 },
    { Sku: "SKU-5", Name: "Thingamajig E", TotalQuantity: 5, AvailableQuantity: 5, ReservedQuantity: 0 }
  ];

  // Insert only if collection empty
  const count = db.products.countDocuments({});
  if (count === 0) {
    db.products.insertMany(seed);
    print(`Inserted ${seed.length} seed products into inventory.products`);
  } else {
    print(`inventory.products already contains ${count} documents; skipping seed`);
  }
} catch (err) {
  print('init-products.js error: ' + err);
}
