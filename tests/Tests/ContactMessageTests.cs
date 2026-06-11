using Domain.Aggregates.Contact;
using Domain.Events;

namespace Tests;

public class ContactMessageTests
{
    [Fact]
    public void Submit_SetsProperties()
    {
        // Act
        var msg = ContactMessage.Submit("Alice", "alice@example.com", "Hello", "Body text");

        // Assert
        Assert.Equal("Alice", msg.SenderName);
        Assert.Equal("alice@example.com", msg.SenderEmail);
        Assert.Equal("Hello", msg.Subject);
        Assert.Equal("Body text", msg.Body);
        Assert.NotEqual(Guid.Empty, msg.Id);
        Assert.True(msg.ReceivedAt <= DateTime.UtcNow);
        Assert.True(msg.ReceivedAt > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Submit_RaisesContactMessageReceivedEvent()
    {
        // Act
        var msg = ContactMessage.Submit("Bob", "bob@example.com", "Subject", "Message body");

        // Assert
        Assert.Single(msg.DomainEvents);
        var evt = Assert.IsType<ContactMessageReceived>(msg.DomainEvents.Single());
        Assert.Equal(msg.SenderName, evt.SenderName);
        Assert.Equal(msg.SenderEmail, evt.SenderEmail);
        Assert.Equal(msg.Subject, evt.Subject);
        Assert.Equal(msg.Body, evt.Message);
        Assert.Equal(msg.ReceivedAt, evt.OccurredAt);
    }

    [Fact]
    public void Submit_EventPayloadMirrorsAggregateFields()
    {
        // Act
        var msg = ContactMessage.Submit("Carol", "carol@example.com", "Re: Proposal", "Please review.");

        // Assert — the event carries its own identity GUID (not the aggregate ID)
        var evt = (ContactMessageReceived)msg.DomainEvents.Single();
        Assert.NotEqual(Guid.Empty, evt.Id);
        Assert.NotEqual(msg.Id, evt.Id); // event Id is independent of aggregate Id
        Assert.Equal("Carol", evt.SenderName);
        Assert.Equal("carol@example.com", evt.SenderEmail);
        Assert.Equal("Re: Proposal", evt.Subject);
        Assert.Equal("Please review.", evt.Message);
        Assert.Equal(msg.ReceivedAt, evt.OccurredAt);
    }

    [Fact]
    public void Submit_EmptySenderName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ContactMessage.Submit("  ", "a@b.com", "Subject", "Body"));
    }

    [Fact]
    public void Submit_EmptySenderEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ContactMessage.Submit("Alice", "", "Subject", "Body"));
    }

    [Fact]
    public void Submit_EmptySubject_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ContactMessage.Submit("Alice", "a@b.com", "   ", "Body"));
    }

    [Fact]
    public void Submit_EmptyBody_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ContactMessage.Submit("Alice", "a@b.com", "Subject", ""));
    }

    [Fact]
    public void Submit_TrimsWhitespace_FromInputs()
    {
        // Act
        var msg = ContactMessage.Submit("  Dave  ", "  dave@example.com  ", "  Hi  ", "  Content  ");

        // Assert
        Assert.Equal("Dave", msg.SenderName);
        Assert.Equal("dave@example.com", msg.SenderEmail);
        Assert.Equal("Hi", msg.Subject);
        Assert.Equal("Content", msg.Body);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesCollection()
    {
        // Arrange
        var msg = ContactMessage.Submit("Eve", "eve@example.com", "Test", "Body");
        Assert.NotEmpty(msg.DomainEvents);

        // Act
        msg.ClearDomainEvents();

        // Assert
        Assert.Empty(msg.DomainEvents);
    }
}
