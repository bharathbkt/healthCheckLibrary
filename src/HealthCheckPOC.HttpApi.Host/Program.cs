using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using HealthCheckPOC.HttpApi.Host;
using System;

var builder = WebApplication.CreateBuilder(args);

// Replace default DI provider with Autofac (Standard for ABP)
builder.Host.UseAutofac();

// Add the root ABP module
await builder.AddApplicationAsync<HealthCheckPOCHttpApiHostModule>();

var app = builder.Build();

// Initialize ABP
await app.InitializeApplicationAsync();

app.Run();
