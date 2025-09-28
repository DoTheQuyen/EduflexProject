// init-mongo.js
db = db.getSiblingDB('EduflexDB');

// Create collections
db.createCollection('users');
db.createCollection('students');
db.createCollection('courses');
db.createCollection('institutions');

// Create indexes
db.users.createIndex({ "email": 1 }, { unique: true });
db.students.createIndex({ "userId": 1 }, { unique: true });

// Insert sample admin user (password will be hashed by your application)
db.users.insertOne({
    email: "admin@eduflex.com",
    passwordHash: "$2a$10$N9qo8uLOickgx2ZMRZoMye6G5zY7JgG5yG5Z7JgG5yG5Z7JgG5yG", // Example hash
    firstName: "Admin",
    lastName: "User",
    role: "Admin",
    isActive: true,
    createdAt: new Date(),
    lastLogin: null
});

// Insert sample institutions
db.institutions.insertMany([
    {
        name: "University of Sydney",
        type: "University",
        location: "Sydney, NSW",
        ranking: 1,
        courses: ["Business", "Engineering", "Medicine"],
        createdAt: new Date()
    },
    {
        name: "University of Melbourne",
        type: "University",
        location: "Melbourne, VIC",
        ranking: 2,
        courses: ["Arts", "Science", "Law"],
        createdAt: new Date()
    }
]);

print("Eduflex MongoDB database initialized successfully!");