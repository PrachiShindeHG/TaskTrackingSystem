using Moq;
using MongoDB.Driver;
using UserService.Models;
using Xunit;

public class UserRepositoryTests
{
    [Fact]
    public async Task CreateAsync_AddsUser()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<User>>();
        mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<User>(), null, default)).Returns(Task.CompletedTask);
        var repo = new MongoUserRepository((Microsoft.Extensions.Options.IOptions<MongoSettings>)mockCollection.Object); // Adjust for constructor

        // Act
        var user = new User { Username = "test" };
        var result = await repo.CreateAsync(user);

        // Assert
        Assert.NotNull(result);
        mockCollection.Verify(c => c.InsertOneAsync(It.IsAny<User>(), null, default), Times.Once);
    }
}