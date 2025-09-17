using Xunit;

[assembly: TestCollectionOrderer("FlexKit.Logging.Tests.Detection.LastCollectionOrderer", "FlexKit.Logging.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]