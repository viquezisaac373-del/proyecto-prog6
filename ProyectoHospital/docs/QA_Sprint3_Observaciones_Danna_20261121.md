# QA de Hospinomenal — Observaciones del 21/11/2026

**Origen:** comentarios de Danna Rojas durante la revisión del Sprint 3.  
**Estado:** propuesta de correcciones en `fix/qa-rn-integridad-20261121`, sin compilar ni certificar en entorno SQL/MVC.  
**Base de comparación:** `develop` del 21/11/2026. Ningún cambio a bases de datos se ejecuta por esta rama.

## Correcciones propuestas en este PR

1. **Fecha de nacimiento al dar de alta un recién nacido:** el servidor rechaza fechas futuras (comportamiento anterior) y ahora también fechas con más de **30 días** de antigüedad. El formulario replica la ventana. **Regla temporal de producto, no plazo legal ni médico confirmado:** su aprobación y un flujo para nacimientos históricos quedan pendientes del PO. **No aplicar esta ventana a la edición de registros ya existentes.**
2. **Selección de madre:** el formulario lista ahora solo expedientes activos con `Genero='Femenino'` y el controlador verifica esa regla de nuevo antes del alta. **Limitación conocida:** el género administrativo NO acredita quién gestó o parió al bebé. Una solución profesional debe registrar una relación materna/gestacional validada, permitir excepciones legítimas y reforzar la integridad en SQL. No interpretar la regla provisional como una afirmación clínica universal.
3. **Teléfonos:** controles internacionales reutilizables en registro y edición de recién nacidos, con código de país, validación de biblioteca y validación adicional con libphonenumber en ASP.NET. Antes de persistir, los valores se normalizan a 8–20 dígitos, compatibles con el esquema actual. Los teléfonos son de contacto del responsable, no necesariamente del bebé.
4. **Ediciones sensibles:** mantenimiento habitual de pacientes no modifica identificación, fecha de nacimiento, género ni sangre, aunque alguien manipule un POST. La edición de nacimientos también preserva género y fecha/hora; mantiene madre y médico. Las correcciones excepcionales autorizadas con motivo y trazabilidad requieren un flujo separado, aún no implementado.
5. **ND en alta general:** se retira del selector de pacientes y se rechaza en el POST. El `ND` técnico permanece para recién nacidos y expedientes históricos; `RHN` es un fenotipo distinto, que requiere compatibilidad SQL.
6. **Apgar y datos clínicos:** se conserva `Apgar1Min` y `Apgar5Min` como puntuaciones enteras 0–10, y como opcionales cuando no se dispone de una evaluación documentada. Se aclara visualmente. No inventar valores médicos para satisfacer un formulario.

## Pruebas que debe ejecutar QA (datos ficticios, solo Hospital_DB4_Integracion)

| ID | Prueba | Resultado esperado |
|---|---|---|
| RN-QA-01 | Alta con fecha de 2005 | Rechazo por antigüedad en MVC; sin filas nuevas |
| RN-QA-02 | Alta con fecha de hace 5 días | Se admite si el resto es válido |
| RN-QA-03 | Alta con fecha de mañana | Se rechaza |
| RN-QA-04 | Selección madre: expediente masculino | No aparece en lista; si se fuerza ID en POST, error |
| RN-QA-05 | Selección madre: expediente femenino activo, médico activo | Se puede continuar |
| RN-QA-06 | Registro con teléfono CR válido y código internacional | Se conserva sin '+' en BD, validación satisfactoria |
| RN-QA-07 | Teléfono inventado o incompleto | No se registra |
| RN-QA-08 | Apgar 0, 7, 10 en ambos controles | Valores válidos |
| RN-QA-09 | Apgar -1, 11 y texto/decimal | Rechazo; no persiste |
| RN-QA-10 | Campo clínico sin resultado | Permanece NULL, no se fuerza un número falso |
| RN-QA-11 | Editar paciente e intentar adulterar ID, género, fecha o sangre con DevTools/POST | La BD conserva valores originales; otros campos autorizados sí pueden cambiar |
| RN-QA-12 | Editar nacimiento e intentar alterar género o fecha/hora | La BD conserva ambas; se permiten cambios clínicos/administrativos ordinarios |
| RN-QA-13 | Alta general con TipoSangre=ND manipulando POST | Rechazo |
| RN-QA-14 | Paciente con TipoSangre=ND previo | Se puede consultar/editar contacto, sin modificar ND accidentalmente |
| RN-QA-15 | Revisión de auditoría | Los cambios permitidos contienen UsuarioId correcto en BitacoraAuditoria |

## Dependencias y riesgos antes de fusionar

- **No hubo compilación ni ejecución de pruebas aquí.** Se necesita `dotnet restore`, `dotnet build ProyectoHospital/ProyectoHospital.slnx` y regresión de QA en Windows/.NET 8/SQL Server.
- El ensamblado requiere `libphonenumber-csharp` 9.0.42; evaluar compatibilidad con la HU12 (#15), que también propone esa dependencia.
- El procedimiento `dbo.usp_RegistrarRecienNacido` y las escrituras directas a SQL todavía necesitan verificar la regla de maternidad y la ventana temporal para ser una defensa **integral**, no solo MVC.
- La regla `Genero=Femenino` es un filtro administrativo provisional; **no resuelve filiación ni capacidad gestacional**. El PO y el responsable clínico deben aprobar un criterio basado en relación de nacimiento, no una inferencia simple del género.
- Un nacimiento legítimo ingresado más de 30 días después requerirá un flujo de registro histórico y autorización. **Ese flujo no existe en este PR.**
- La congelación de identificación/tipo sanguíneo/fecha/género exige un flujo de corrección excepcional controlado que permita rectificar errores médicos o administrativos sin alterar silenciosamente el historial. Tampoco existe aquí.
- La actualización desde `ND` a grupo sanguíneo confirmado debe planificarse de forma específica, con resultado de laboratorio y auditoría. No se habilita por la edición ordinaria.
- La rama `develop` tiene un script base que no incluye `RHN` ni la auditoría de pacientes: validar la migración real de integración y coordinar versionado con José Steven antes de declarar despliegue reproducible.
- En caso de que falle la carga de librería telefónica desde CDN, el formulario se mantiene inhabilitado en lugar de permitir datos no verificados; contemplar empaquetar dependencias localmente para resiliencia.

## Registro sugerido para UTC Agile (sin alterar Sprint 3 retrospectivamente)

**Tipo:** Defecto/Mejora QA, pendiente de priorización del Product Owner.  
**Título:** Fortalecer controles de integridad en recién nacidos y edición de pacientes.  
**Descripción:** Limitar antigüedad en alta ordinaria, corregir selección de madre, validar teléfonos, restringir edición de datos sensibles y mejorar validación/presentación de Apgar sin exigir datos clínicos ficticios.  
**Criterios de aceptación:** RN-QA-01 a RN-QA-15, luego de la validación del alcance por el PO.  
**Estado a registrar:** Encontrado y con corrección propuesta en rama; **no terminado ni aprobado por QA**.  
**Implicación Scrum:** requiere estimación, dependencia de SQL, criterios del PO, y decisión de incorporación al Sprint 3 o al siguiente; no sumar puntos retrospectivos sin acuerdo.

## Ajuste de sexo registrado al nacer (21/11/2026)

Observación de QA: el selector decía "Género" y ofrecía "Otro". Se propone distinguir
**sexo registrado al nacer**, sin inferir identidad de género ni diagnóstico.

- Formulario de alta de RN: **Masculino / Femenino / Indeterminado
  (pendiente de valoración clínica)**.
- El valor guardado en `Pacientes.Genero` para la última opción es
  **`Indeterminado`**, no `Otro`. Es una adaptación provisional del esquema
  histórico; en una refactorización futura conviene separar `SexoAlNacer`
  de los datos administrativos de género.
- Edición de RN: dato de solo lectura. Los antiguos `Otro` siguen mostrándose
  como `Otro (dato histórico; requiere revisión)`; no se reinterpretan.
- El POST de registro rechaza `Otro` incluso cuando alguien lo envía de forma
  manipulada. Las ediciones antiguas pueden seguir leyendo ese valor.
- La vista de pacientes acepta `Indeterminado` al editar datos administrativos
  de un RN existente, pero no cambia el dato en el mantenimiento ordinario.

### Dependencia de SQL Server

Se incluyó la migración
`ProyectoHospital/database/migrations/20261121_RN_SexoIndeterminado_SOLO_INTEGRACION.sql`.
Su ejecución es **manual**, exclusivamente con `Hospital_DB4_Integracion`
seleccionada y tras revisión por José Steven. Solo amplía
`CK_Pacientes_Genero` y conserva `Otro`; nunca debe aplicarse a
`Hospital_DB4` principal. Una instalación nueva obtiene el mismo catálogo
del script base actualizado, sin reejecutarlo en una base existente.

**Pruebas adicionales RN-QA-16 a RN-QA-19:**

- RN-QA-16: verificar que el alta ofrece Indeterminado y NO Otro.
- RN-QA-17: comprobar que un RN Indeterminado se guarda, consulta y edita sin
  cambiar involuntariamente el valor; exige migración SQL aplicada.
- RN-QA-18: POST manipulado con `Genero=Otro` en nueva alta: rechazo.
- RN-QA-19: verificar que los pacientes históricos con `Otro` persisten y
  pueden consultarse sin alteración.

**Estado:** código propuesto, compilación y pruebas funcionales de esta adición
pendientes. El Product Owner debe aprobar terminología y proceso de
confirmación/corrección clínica posterior.
