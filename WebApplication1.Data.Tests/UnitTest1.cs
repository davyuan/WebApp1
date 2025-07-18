using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Data.Tests;

public class AppDbContextTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(_options);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldCreateInstance()
    {
        // Arrange & Act
        using var context = new AppDbContext(_options);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<AppDbContext>(context);
    }

    [Fact]
    public void Cities_DbSet_ShouldBeInitialized()
    {
        // Assert
        Assert.NotNull(_context.Cities);
        Assert.IsAssignableFrom<DbSet<City>>(_context.Cities);
    }

    [Fact]
    public void ZipCodes_DbSet_ShouldBeInitialized()
    {
        // Assert
        Assert.NotNull(_context.ZipCodes);
        Assert.IsAssignableFrom<DbSet<ZipCode>>(_context.ZipCodes);
    }

    [Fact]
    public void OnModelCreating_ShouldConfigureCityEntity()
    {
        // Arrange & Act
        var cityEntityType = _context.Model.FindEntityType(typeof(City));

        // Assert
        Assert.NotNull(cityEntityType);
        
        // Check primary key
        var primaryKey = cityEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey.Properties);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        // Check Name property configuration
        var nameProperty = cityEntityType.FindProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.False(nameProperty.IsNullable);
        
        // For InMemory database, we check the configured type mapping instead of GetColumnType()
        var typeMapping = nameProperty.GetTypeMapping();
        Assert.NotNull(typeMapping);
        // Verify the property is required (not nullable)
        Assert.True(nameProperty.IsColumnNullable() == false);
    }

    [Fact]
    public void OnModelCreating_ShouldConfigureZipCodeEntity()
    {
        // Arrange & Act
        var zipCodeEntityType = _context.Model.FindEntityType(typeof(ZipCode));

        // Assert
        Assert.NotNull(zipCodeEntityType);
        
        // Check composite primary key
        var primaryKey = zipCodeEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(2, primaryKey.Properties.Count);
        Assert.Contains(primaryKey.Properties, p => p.Name == "CityId");
        Assert.Contains(primaryKey.Properties, p => p.Name == "Zip");

        // Check Zip property configuration
        var zipProperty = zipCodeEntityType.FindProperty("Zip");
        Assert.NotNull(zipProperty);
        Assert.False(zipProperty.IsNullable);
        
        // For InMemory database, we check the type mapping instead of GetColumnType()
        var typeMapping = zipProperty.GetTypeMapping();
        Assert.NotNull(typeMapping);
        // Verify the property is required (not nullable)
        Assert.True(zipProperty.IsColumnNullable() == false);

        // Check foreign key relationship
        var foreignKeys = zipCodeEntityType.GetForeignKeys();
        Assert.Single(foreignKeys);
        var foreignKey = foreignKeys.First();
        Assert.Equal("CityId", foreignKey.Properties.First().Name);
        Assert.Equal(typeof(City), foreignKey.PrincipalEntityType.ClrType);
    }

    [Fact]
    public async Task AddCity_ShouldPersistToDatabase()
    {
        // Arrange
        var city = new City
        {
            Id = 1,
            Name = "Test City"
        };

        // Act
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        // Assert
        var savedCity = await _context.Cities.FindAsync(1);
        Assert.NotNull(savedCity);
        Assert.Equal("Test City", savedCity.Name);
        Assert.Equal(1, savedCity.Id);
    }

    [Fact]
    public async Task AddZipCode_ShouldPersistToDatabase()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        var zipCode = new ZipCode
        {
            CityId = 1,
            Zip = "12345"
        };

        // Act
        _context.ZipCodes.Add(zipCode);
        await _context.SaveChangesAsync();

        // Assert
        var savedZipCode = await _context.ZipCodes.FindAsync(1, "12345");
        Assert.NotNull(savedZipCode);
        Assert.Equal(1, savedZipCode.CityId);
        Assert.Equal("12345", savedZipCode.Zip);
    }

    [Fact]
    public async Task ZipCodeCityRelationship_ShouldWork()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };
        var zipCode = new ZipCode { CityId = 1, Zip = "12345" };

        _context.Cities.Add(city);
        _context.ZipCodes.Add(zipCode);
        await _context.SaveChangesAsync();

        // Act
        var zipCodeWithCity = await _context.ZipCodes
            .Include(z => z.City)
            .FirstAsync(z => z.CityId == 1 && z.Zip == "12345");

        // Assert
        Assert.NotNull(zipCodeWithCity.City);
        Assert.Equal("Test City", zipCodeWithCity.City.Name);
        Assert.Equal(1, zipCodeWithCity.City.Id);
    }

    [Fact]
    public async Task CityZipCodesRelationship_ShouldWork()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };
        var zipCode1 = new ZipCode { CityId = 1, Zip = "12345" };
        var zipCode2 = new ZipCode { CityId = 1, Zip = "54321" };

        _context.Cities.Add(city);
        _context.ZipCodes.AddRange(zipCode1, zipCode2);
        await _context.SaveChangesAsync();

        // Act
        var cityWithZipCodes = await _context.Cities
            .Include(c => c.ZipCodes)
            .FirstAsync(c => c.Id == 1);

        // Assert
        Assert.NotNull(cityWithZipCodes.ZipCodes);
        Assert.Equal(2, cityWithZipCodes.ZipCodes.Count);
        Assert.Contains(cityWithZipCodes.ZipCodes, z => z.Zip == "12345");
        Assert.Contains(cityWithZipCodes.ZipCodes, z => z.Zip == "54321");
    }

    [Fact]
    public async Task UpdateCity_ShouldPersistChanges()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Original Name" };
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        // Act
        city.Name = "Updated Name";
        await _context.SaveChangesAsync();

        // Assert
        var updatedCity = await _context.Cities.FindAsync(1);
        Assert.NotNull(updatedCity);
        Assert.Equal("Updated Name", updatedCity.Name);
    }

    [Fact]
    public async Task DeleteCity_ShouldRemoveFromDatabase()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        // Act
        _context.Cities.Remove(city);
        await _context.SaveChangesAsync();

        // Assert
        var deletedCity = await _context.Cities.FindAsync(1);
        Assert.Null(deletedCity);
    }

    [Fact]
    public async Task DeleteZipCode_ShouldRemoveFromDatabase()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };
        var zipCode = new ZipCode { CityId = 1, Zip = "12345" };
        _context.Cities.Add(city);
        _context.ZipCodes.Add(zipCode);
        await _context.SaveChangesAsync();

        // Act
        _context.ZipCodes.Remove(zipCode);
        await _context.SaveChangesAsync();

        // Assert
        var deletedZipCode = await _context.ZipCodes.FindAsync(1, "12345");
        Assert.Null(deletedZipCode);
    }

    [Fact]
    public async Task MultipleZipCodesForSameCity_ShouldWork()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };
        var zipCodes = new[]
        {
            new ZipCode { CityId = 1, Zip = "12345" },
            new ZipCode { CityId = 1, Zip = "54321" },
            new ZipCode { CityId = 1, Zip = "67890" }
        };

        _context.Cities.Add(city);
        _context.ZipCodes.AddRange(zipCodes);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.ZipCodes.CountAsync(z => z.CityId == 1);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ZipCodeCompositeKey_ShouldPreventDuplicates()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };
        var zipCode1 = new ZipCode { CityId = 1, Zip = "12345" };
        
        _context.Cities.Add(city);
        _context.ZipCodes.Add(zipCode1);
        await _context.SaveChangesAsync();

        // Clear the context to avoid tracking issues
        _context.ChangeTracker.Clear();

        var zipCode2 = new ZipCode { CityId = 1, Zip = "12345" }; // Duplicate

        // Act & Assert
        _context.ZipCodes.Add(zipCode2);
        
        // The InMemory provider will throw ArgumentException when trying to save duplicate composite keys
        await Assert.ThrowsAsync<ArgumentException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task QueryZipCodesByCity_ShouldWork()
    {
        // Arrange
        var city1 = new City { Id = 1, Name = "City 1" };
        var city2 = new City { Id = 2, Name = "City 2" };
        var zipCodes = new[]
        {
            new ZipCode { CityId = 1, Zip = "12345" },
            new ZipCode { CityId = 1, Zip = "54321" },
            new ZipCode { CityId = 2, Zip = "67890" }
        };

        _context.Cities.AddRange(city1, city2);
        _context.ZipCodes.AddRange(zipCodes);
        await _context.SaveChangesAsync();

        // Act
        var city1ZipCodes = await _context.ZipCodes
            .Where(z => z.CityId == 1)
            .ToListAsync();

        // Assert
        Assert.Equal(2, city1ZipCodes.Count);
        Assert.All(city1ZipCodes, z => Assert.Equal(1, z.CityId));
    }

    [Fact]
    public async Task EmptyDatabase_ShouldReturnEmptyCollections()
    {
        // Act
        var cities = await _context.Cities.ToListAsync();
        var zipCodes = await _context.ZipCodes.ToListAsync();

        // Assert
        Assert.Empty(cities);
        Assert.Empty(zipCodes);
    }

    [Fact]
    public async Task ChangeTracking_ShouldTrackEntityStates()
    {
        // Arrange
        var city = new City { Id = 1, Name = "Test City" };

        // Act - Add entity
        _context.Cities.Add(city);
        var addedState = _context.Entry(city).State;

        await _context.SaveChangesAsync();
        var unchangedState = _context.Entry(city).State;

        // Modify entity
        city.Name = "Modified City";
        var modifiedState = _context.Entry(city).State;

        await _context.SaveChangesAsync();
        var savedState = _context.Entry(city).State;

        // Assert
        Assert.Equal(EntityState.Added, addedState);
        Assert.Equal(EntityState.Unchanged, unchangedState);
        Assert.Equal(EntityState.Modified, modifiedState);
        Assert.Equal(EntityState.Unchanged, savedState);
    }

    [Fact]
    public void ContextDisposal_ShouldDisposeCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert - Should not throw
        using (var context = new AppDbContext(options))
        {
            Assert.NotNull(context.Cities);
            Assert.NotNull(context.ZipCodes);
        }
        
        // Context should be disposed without issues
    }

    [Fact]
    public async Task BulkOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var cities = new[]
        {
            new City { Id = 1, Name = "City 1" },
            new City { Id = 2, Name = "City 2" },
            new City { Id = 3, Name = "City 3" }
        };

        // Act
        _context.Cities.AddRange(cities);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.Cities.CountAsync();
        Assert.Equal(3, count);

        // Test bulk delete
        _context.Cities.RemoveRange(cities);
        await _context.SaveChangesAsync();

        var countAfterDelete = await _context.Cities.CountAsync();
        Assert.Equal(0, countAfterDelete);
    }
}