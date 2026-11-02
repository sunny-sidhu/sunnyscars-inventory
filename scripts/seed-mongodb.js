// Seeds the car inventory collection from SunnysCars.Inventory.Api/Data/cars.json.
//
// Usage (the database comes from the connection string):
//   mongosh "mongodb://localhost:27017/sunnys_cars" scripts/seed-mongodb.js
//
// Safe to re-run: cars are only inserted into an empty collection, and any prices stored as
// doubles (for example by a plain mongoimport) are converted to Decimal128 to match the API.

const path = require('path');

const collection = db.getCollection('inventory');
const seedFile = path.join(__dirname, '..', 'SunnysCars.Inventory.Api', 'Data', 'cars.json');
const toPrice = price => NumberDecimal(Number(price).toFixed(2));

if (collection.countDocuments() === 0) {
  const cars = JSON.parse(fs.readFileSync(seedFile, 'utf8'));
  collection.insertMany(cars.map(car => ({ ...car, price: toPrice(car.price) })));
  print(`Inserted ${cars.length} cars from ${seedFile}.`);
} else {
  print('Collection already has cars; skipping insert.');
}

const doublePrices = collection.find({ price: { $type: 'double' } }, { price: 1 }).toArray();
if (doublePrices.length > 0) {
  collection.bulkWrite(doublePrices.map(car => ({
    updateOne: { filter: { _id: car._id }, update: { $set: { price: toPrice(car.price) } } }
  })));
  print(`Converted ${doublePrices.length} double prices to Decimal128.`);
}

print(`${db.getName()}.${collection.getName()} now holds ${collection.countDocuments()} cars.`);
