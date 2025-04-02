using Couchbase.Extensions.DependencyInjection;
using Signify.Dps.LabResultApi.CouchbasePoc;
using Signify.Dps.LabResultApi.CouchbasePoc.Configs;
using Signify.Dps.LabResultApi.CouchbasePoc.Data;
using Signify.Dps.LabResultApi.CouchbasePoc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCouchbase(builder.Configuration.GetSection("Couchbase"))
    .AddCouchbaseBucket<INamedBucketProvider>(CouchbaseConstants.Bucket);

builder.Services.AddSingleton<IDocumentControllerConfig>(
    builder.Configuration
        .GetSection(DocumentControllerConfig.ConfigName)
        .Get<DocumentControllerConfig>());

builder.Services.AddHostedService<CouchbaseHostedService>();
builder.Services.AddScoped<ICouchbaseCollectionProvider, CouchbaseCollectionProvider>();
builder.Services.AddScoped<ILabResultRepository, LabResultRepository>();
builder.Services.AddScoped<ILabDocumentRepository, LabDocumentRepository>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapDefaultControllerRoute();
app.MapControllers();

await app.RunAsync();
