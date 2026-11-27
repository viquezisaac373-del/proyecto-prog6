using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Hospinomenal.WcfService.Services;

var builder = WebApplication.CreateBuilder(args);

// Exclusivamente local: SOAP sin autenticación de transporte para la demo.
// No publicar este servicio a Internet sin HTTPS y controles de autenticación.
builder.WebHost.UseUrls(builder.Configuration["Wcf:ListenUrl"] ?? "http://127.0.0.1:5108");
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddTransient<HorariosMedicosService>();

var app = builder.Build();

app.UseServiceModel(services =>
{
    services.AddService<HorariosMedicosService>();
    services.AddServiceEndpoint<HorariosMedicosService, IHorariosMedicosService>(
        new BasicHttpBinding(), "/HorariosMedicos.svc");
});

var metadata = app.Services.GetRequiredService<ServiceMetadataBehavior>();
metadata.HttpGetEnabled = true;

app.Run();
