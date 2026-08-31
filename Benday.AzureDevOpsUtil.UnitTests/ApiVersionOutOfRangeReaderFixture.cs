using Benday.AzureDevOpsUtil.Api.ApiVersioning;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ApiVersionOutOfRangeReaderFixture
{
    /// <summary>
    /// Verbatim shape of the rejection, captured from a live collection.
    /// </summary>
    private const string OutOfRangeBody =
        "{\"$id\":\"1\",\"innerException\":null,\"message\":\"The requested REST API version of 7.0 " +
        "is out of range for this server. The latest REST API version this server supports is 5.1.\"," +
        "\"typeName\":\"Microsoft.VisualStudio.Services.WebApi.VssVersionOutOfRangeException, " +
        "Microsoft.VisualStudio.Services.WebApi\",\"typeKey\":\"VssVersionOutOfRangeException\"," +
        "\"errorCode\":0,\"eventId\":3000}";

    [TestMethod]
    public void RecognisesTheRejection()
    {
        // arrange & act
        var actual = ApiVersionOutOfRangeReader.IsVersionOutOfRange(OutOfRangeBody);

        // assert
        Assert.IsTrue(actual, "should have recognised the rejection");
    }

    [TestMethod]
    public void ReadsTheCeilingOutOfTheMessage()
    {
        // arrange & act
        var read = ApiVersionOutOfRangeReader.TryReadSupportedVersion(OutOfRangeBody, out var actual);

        // assert
        Assert.IsTrue(read, "should have read the ceiling");
        Assert.AreEqual<string>("5.1", actual.ToString(), "the sentence period is not part of the number");
    }

    [TestMethod]
    public void IgnoresAnUnrelatedBadRequest()
    {
        // arrange
        var body = "{\"message\":\"TF401019: The Git repository does not exist.\"," +
            "\"typeKey\":\"GitRepositoryNotFoundException\"}";

        // act
        var actual = ApiVersionOutOfRangeReader.IsVersionOutOfRange(body);

        // assert
        Assert.IsFalse(actual, "not a version problem");
    }

    [TestMethod]
    public void HandlesARejectionThatNamesNoCeiling()
    {
        // arrange -- the message is localized, so the sentence may not be there
        var body = "{\"message\":\"is out of range for this server\"," +
            "\"typeKey\":\"VssVersionOutOfRangeException\"}";

        // act
        var recognised = ApiVersionOutOfRangeReader.IsVersionOutOfRange(body);
        var read = ApiVersionOutOfRangeReader.TryReadSupportedVersion(body, out _);

        // assert
        Assert.IsTrue(recognised, "still a version rejection");
        Assert.IsFalse(read, "but it names no ceiling");
    }
}
