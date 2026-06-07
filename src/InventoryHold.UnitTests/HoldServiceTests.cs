using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using InventoryHold.Domain.Services;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Messaging;
using InventoryHold.Domain.Caching;
using InventoryHold.Domain.Models;

namespace InventoryHold.UnitTests
{
    public class HoldServiceTests
    {
        private readonly Mock<IHoldRepository> _holdRepo = new();
        private readonly Mock<IInventoryRepository> _inventoryRepo = new();
        private readonly Mock<IMessageBus> _bus = new();
        private readonly Mock<ICache> _cache = new();
        private readonly Mock<IOutboxRepository> _outbox = new();

        private HoldService CreateService()
            => new HoldService(_holdRepo.Object, _inventoryRepo.Object, _bus.Object, _cache.Object, _outbox.Object);

        [Fact]
        public async Task CreateHold_PersistsHold_And_EnqueuesOutbox_And_InvalidatesCache()
        {
            // Arrange
            var sku = "SKU-UT-1";
            var qty = 1;

            var product = new ProductInventory { Sku = sku, Name = "Test", AvailableQuantity = 5, TotalQuantity = 5 };
            _inventoryRepo.Setup(x => x.GetBySkuAsync(sku)).ReturnsAsync(product);
            _inventoryRepo.Setup(x => x.TryReserveAsync(sku, qty, It.IsAny<string?>())).ReturnsAsync(product);

            _holdRepo.Setup(x => x.CreateWithOutboxAsync(It.IsAny<Hold>(), It.IsAny<OutboxEntry>())).ReturnsAsync(true);

            var svc = CreateService();

            // Act
            var result = await svc.CreateHoldAsync(sku, qty);

            // Assert
            Assert.NotNull(result);
            _inventoryRepo.Verify(x => x.TryReserveAsync(sku, qty, It.IsAny<string?>()), Times.Once);
            _holdRepo.Verify(x => x.CreateWithOutboxAsync(It.IsAny<Hold>(), It.IsAny<OutboxEntry>()), Times.Once);
            _cache.Verify(x => x.RemoveAsync(It.Is<string>(k => k.Contains("hold:"))), Times.Once);
            _cache.Verify(x => x.RemoveAsync("inventory:list"), Times.Once);
        }

        [Fact]
        public async Task CreateHold_InsufficientStock_ReturnsNull_And_NoPersistenceOrOutbox()
        {
            // Arrange
            var sku = "SKU-UT-2";
            var qty = 10;

            _inventoryRepo.Setup(x => x.GetBySkuAsync(sku)).ReturnsAsync((ProductInventory?)null);

            var svc = CreateService();

            // Act
            var result = await svc.CreateHoldAsync(sku, qty);

            // Assert
            Assert.Null(result);
            _holdRepo.Verify(x => x.CreateAsync(It.IsAny<Hold>()), Times.Never);
            _outbox.Verify(x => x.CreateAsync(It.IsAny<OutboxEntry>()), Times.Never);
        }

        [Fact]
        public async Task ReleaseHold_Success_RestoresInventory_And_EnqueuesOutbox_And_InvalidatesCache()
        {
            // Arrange
            var holdId = Guid.NewGuid().ToString();
            var sku = "SKU-UT-3";
            var qty = 2;
            var existingHold = new Hold { Id = holdId, Sku = sku, Quantity = qty, Status = HoldStatus.Active };

            _holdRepo.Setup(x => x.GetByIdAsync(holdId)).ReturnsAsync(existingHold);
            _inventoryRepo.Setup(x => x.ReleaseReservedQuantityAsync(sku, qty, It.IsAny<string?>())).ReturnsAsync(true);
            _holdRepo.Setup(x => x.ReleaseWithOutboxAsync(holdId, It.IsAny<OutboxEntry>())).ReturnsAsync(true);

            var svc = CreateService();

            // Act
            var ok = await svc.ReleaseHoldAsync(holdId);

            // Assert
            Assert.True(ok);
            _inventoryRepo.Verify(x => x.ReleaseReservedQuantityAsync(sku, qty, It.IsAny<string?>()), Times.Once);
            _holdRepo.Verify(x => x.ReleaseWithOutboxAsync(holdId, It.IsAny<OutboxEntry>()), Times.Once);
            _cache.Verify(x => x.RemoveAsync(It.Is<string>(s => s.Contains($"hold:{holdId}"))), Times.Once);
        }

        [Fact]
        public async Task ReleaseHold_NotFound_ReturnsFalse()
        {
            // Arrange
            var holdId = Guid.NewGuid().ToString();
            _holdRepo.Setup(x => x.GetByIdAsync(holdId)).ReturnsAsync((Hold?)null);

            var svc = CreateService();

            // Act
            var ok = await svc.ReleaseHoldAsync(holdId);

            // Assert
            Assert.False(ok);
            _inventoryRepo.Verify(x => x.ReleaseReservedQuantityAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
            _outbox.Verify(x => x.CreateAsync(It.IsAny<OutboxEntry>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateHold_NullOrWhiteSpaceSku_ThrowsArgumentException(string? invalidSku)
        {
            // Arrange
            var svc = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateHoldAsync(invalidSku!, 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task CreateHold_ZeroOrNegativeQuantity_ThrowsArgumentException(int invalidQty)
        {
            // Arrange
            var svc = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateHoldAsync("SKU-1", invalidQty));
            Assert.Equal("quantity", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ReleaseHold_NullOrWhiteSpaceId_ReturnsFalse(string? invalidHoldId)
        {
            // Arrange
            var svc = CreateService();

            // Act
            var ok = await svc.ReleaseHoldAsync(invalidHoldId!);

            // Assert
            Assert.False(ok);
        }

        [Fact]
        public async Task ReleaseHold_InactiveStatus_ReturnsFalse()
        {
            // Arrange
            var holdId = Guid.NewGuid().ToString();
            var sku = "SKU-UT-4";
            var existingHold = new Hold { Id = holdId, Sku = sku, Quantity = 2, Status = HoldStatus.Released };

            _holdRepo.Setup(x => x.GetByIdAsync(holdId)).ReturnsAsync(existingHold);

            var svc = CreateService();

            // Act
            var ok = await svc.ReleaseHoldAsync(holdId);

            // Assert
            Assert.False(ok);
            _inventoryRepo.Verify(x => x.ReleaseReservedQuantityAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
            _holdRepo.Verify(x => x.ReleaseAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateHold_Success_WithCustomHoldSecondsAndLocationAndOwner()
        {
            // Arrange
            var sku = "SKU-UT-5";
            var qty = 3;
            var location = "Loc-A";
            var owner = "Owner-1";
            var holdSeconds = 600;

            var product = new ProductInventory { Sku = sku, Name = "Test", AvailableQuantity = 10, TotalQuantity = 10 };
            _inventoryRepo.Setup(x => x.GetBySkuAsync(sku)).ReturnsAsync(product);
            _inventoryRepo.Setup(x => x.TryReserveAsync(sku, qty, location)).ReturnsAsync(product);

            Hold? savedHold = null;
            _holdRepo.Setup(x => x.CreateWithOutboxAsync(It.IsAny<Hold>(), It.IsAny<OutboxEntry>()))
                .Callback<Hold, OutboxEntry>((h, _) => savedHold = h)
                .ReturnsAsync(true);

            var svc = CreateService();

            // Act
            var result = await svc.CreateHoldAsync(sku, qty, location, owner, holdSeconds);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(savedHold);
            Assert.Equal(sku, savedHold.Sku);
            Assert.Equal(qty, savedHold.Quantity);
            Assert.Equal(location, savedHold.Location);
            Assert.Equal(owner, savedHold.Owner);
            Assert.Equal(HoldStatus.Active, savedHold.Status);
            
            // Check that the expiration is set correctly
            var diff = savedHold.ExpiresAt - savedHold.CreatedAt;
            Assert.True(Math.Abs(diff.TotalSeconds - holdSeconds) < 2);

            _inventoryRepo.Verify(x => x.TryReserveAsync(sku, qty, location), Times.Once);
            _holdRepo.Verify(x => x.CreateWithOutboxAsync(It.IsAny<Hold>(), It.IsAny<OutboxEntry>()), Times.Once);
            _cache.Verify(x => x.RemoveAsync($"hold:{result.Id}"), Times.Once);
        }

        [Fact]
        public async Task ReleaseHold_MarkReleaseFailed_RollsBackInventoryReservation_And_ReturnsFalse()
        {
            // Arrange
            var holdId = Guid.NewGuid().ToString();
            var sku = "SKU-UT-6";
            var qty = 4;
            var location = "Loc-B";
            var existingHold = new Hold { Id = holdId, Sku = sku, Quantity = qty, Status = HoldStatus.Active, Location = location };

            _holdRepo.Setup(x => x.GetByIdAsync(holdId)).ReturnsAsync(existingHold);
            _inventoryRepo.Setup(x => x.ReleaseReservedQuantityAsync(sku, qty, location)).ReturnsAsync(true);
            
            // Simulate DB failure where marking the hold released in the database returns false
            _holdRepo.Setup(x => x.ReleaseWithOutboxAsync(holdId, It.IsAny<OutboxEntry>())).ReturnsAsync(false);
            
            // Mock rollback operation
            _inventoryRepo.Setup(x => x.TryReserveAsync(sku, qty, location)).ReturnsAsync(new ProductInventory());

            var svc = CreateService();

            // Act
            var ok = await svc.ReleaseHoldAsync(holdId);

            // Assert
            Assert.False(ok);
            _inventoryRepo.Verify(x => x.ReleaseReservedQuantityAsync(sku, qty, location), Times.Once);
            _holdRepo.Verify(x => x.ReleaseWithOutboxAsync(holdId, It.IsAny<OutboxEntry>()), Times.Once);
            
            // Verify rollback: re-reserving should have been called
            _inventoryRepo.Verify(x => x.TryReserveAsync(sku, qty, location), Times.Once);
            
            // Ensure no cache invalidation was done because of failure
            _cache.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExpireHold_Success_RestoresInventory_And_EnqueuesOutbox_And_InvalidatesCache()
        {
            // Arrange
            var holdId = Guid.NewGuid().ToString();
            var sku = "SKU-UT-7";
            var qty = 2;
            var existingHold = new Hold { Id = holdId, Sku = sku, Quantity = qty, Status = HoldStatus.Active };

            _holdRepo.Setup(x => x.GetByIdAsync(holdId)).ReturnsAsync(existingHold);
            _inventoryRepo.Setup(x => x.ReleaseReservedQuantityAsync(sku, qty, It.IsAny<string?>())).ReturnsAsync(true);
            _holdRepo.Setup(x => x.ExpireWithOutboxAsync(holdId, It.IsAny<OutboxEntry>())).ReturnsAsync(true);

            var svc = CreateService();

            // Act
            var ok = await svc.ExpireHoldAsync(holdId);

            // Assert
            Assert.True(ok);
            _inventoryRepo.Verify(x => x.ReleaseReservedQuantityAsync(sku, qty, It.IsAny<string?>()), Times.Once);
            _holdRepo.Verify(x => x.ExpireWithOutboxAsync(holdId, It.IsAny<OutboxEntry>()), Times.Once);
            _cache.Verify(x => x.RemoveAsync(It.Is<string>(s => s.Contains($"hold:{holdId}"))), Times.Once);
        }

        [Fact]
        public async Task ExpireHold_MarkExpiredFailed_RollsBackInventoryReservation_And_ReturnsFalse()
        {
            // Arrange
            var holdId = Guid.NewGuid().ToString();
            var sku = "SKU-UT-8";
            var qty = 3;
            var existingHold = new Hold { Id = holdId, Sku = sku, Quantity = qty, Status = HoldStatus.Active };

            _holdRepo.Setup(x => x.GetByIdAsync(holdId)).ReturnsAsync(existingHold);
            _inventoryRepo.Setup(x => x.ReleaseReservedQuantityAsync(sku, qty, It.IsAny<string?>())).ReturnsAsync(true);
            _holdRepo.Setup(x => x.ExpireWithOutboxAsync(holdId, It.IsAny<OutboxEntry>())).ReturnsAsync(false);
            _inventoryRepo.Setup(x => x.TryReserveAsync(sku, qty, It.IsAny<string?>())).ReturnsAsync(new ProductInventory());

            var svc = CreateService();

            // Act
            var ok = await svc.ExpireHoldAsync(holdId);

            // Assert
            Assert.False(ok);
            _inventoryRepo.Verify(x => x.ReleaseReservedQuantityAsync(sku, qty, It.IsAny<string?>()), Times.Once);
            _holdRepo.Verify(x => x.ExpireWithOutboxAsync(holdId, It.IsAny<OutboxEntry>()), Times.Once);
            _inventoryRepo.Verify(x => x.TryReserveAsync(sku, qty, It.IsAny<string?>()), Times.Once);
            _cache.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExpireHold_InactiveStatus_ReturnsFalse()
        {
            // Arrange
            var holdId = Guid.NewGuid().ToString();
            var existingHold = new Hold { Id = holdId, Sku = "SKU-9", Quantity = 1, Status = HoldStatus.Expired };

            _holdRepo.Setup(x => x.GetByIdAsync(holdId)).ReturnsAsync(existingHold);

            var svc = CreateService();

            // Act
            var ok = await svc.ExpireHoldAsync(holdId);

            // Assert
            Assert.False(ok);
            _inventoryRepo.Verify(x => x.ReleaseReservedQuantityAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
            _holdRepo.Verify(x => x.ExpireWithOutboxAsync(It.IsAny<string>(), It.IsAny<OutboxEntry>()), Times.Never);
        }
    }
}

