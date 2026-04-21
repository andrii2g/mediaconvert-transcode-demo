using System.Text.Json;
using Amazon.MediaConvert;
using Amazon.S3;
using Demo.UploadApi.Options;
using Microsoft.Extensions.Options;

namespace Demo.UploadApi.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.InputBucket), "Storage:InputBucket is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.OutputBucket), "Storage:OutputBucket is required.")
            .ValidateOnStart();

        services
            .AddOptions<MediaConvertOptions>()
            .Bind(configuration.GetSection(MediaConvertOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "MediaConvert:Endpoint is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RoleArn), "MediaConvert:RoleArn is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Template.Name), "MediaConvert:Template:Name is required.")
            .Validate(options => options.Template.OutputGroups.Count > 0, "MediaConvert:Template:OutputGroups must contain at least one item.")
            .Validate(
                options => options.Template.OutputGroups.All(group =>
                    !string.IsNullOrWhiteSpace(group.Name) &&
                    !string.IsNullOrWhiteSpace(group.Prefix) &&
                    !string.IsNullOrWhiteSpace(group.GroupType)),
                "Every MediaConvert:Template:OutputGroups item must define Name, Prefix, and GroupType.")
            .ValidateOnStart();

        services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });

        services.AddSingleton<SourceObjectValidator>();
        services.AddSingleton<IS3UploadService, S3UploadService>();
        services.AddSingleton<IManifestStore, S3ManifestStore>();
        services.AddSingleton<ITranscodeOrchestrator, MediaConvertOrchestrator>();

        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
        services.AddSingleton<IAmazonMediaConvert>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MediaConvertOptions>>().Value;
            return new AmazonMediaConvertClient(new AmazonMediaConvertConfig
            {
                ServiceURL = options.Endpoint
            });
        });

        return services;
    }
}
