// Seeds the car inventory collection from SunnysCars.Inventory.Api/Data/cars.json.
//
// Usage:
//   mongosh "mongodb://localhost:27017" scripts/seed-mongodb.js
//   mongosh "<Azure DocumentDB connection string without the password>" --username <user> scripts/seed-mongodb.js
//     (mongosh prompts for the password, so it never lands in shell history)
//
// The database and collection default to the API's (sunnys_cars.inventory). Override them with the
// SEED_DATABASE and SEED_COLLECTION environment variables. They are not taken from the connection
// string, because a database in the URL path also changes which database the login authenticates against.
//
// Safe to re-run: cars are only inserted into an empty collection, and any prices stored as
// doubles (for example by a plain mongoimport) are converted to Decimal128 to match the API.

const path = require('path');

const database = db.getSiblingDB(process.env.SEED_DATABASE || 'sunnys_cars');
const collection = database.getCollection(process.env.SEED_COLLECTION || 'inventory');
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

print(`${database.getName()}.${collection.getName()} now holds ${collection.countDocuments()} cars.`);
