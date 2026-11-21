# HU12 — Tipos de identificación de pacientes (decisión técnica preliminar)

**Estado:** componente de validación de formato preparado, **no integrado aún al formulario ni a la BD**. No utilizar como funcionalidad terminada en la demostración del lunes 23/11/2026.

## Evidencia institucional

- La CCSS identifica la cédula nacional mediante **9 dígitos** y el DIMEX mediante **12 dígitos** (manual de Oficina Virtual): https://www.ccss.sa.cr/arc/sigesco/7d9fc.pdf
- La Dirección General de Migración y Extranjería reconoce el DIMEX como documento migratorio de identificación: https://www.migracion.go.cr/Documentos%20compartidos/Normativa%20Legal%20Migratoria/Circular%20DIMEX%20que%20se%20encuentran%20actualmente%20vigentes.pdf
- La CCSS contempla cédula nacional, TIM, DIMEX y pasaporte en distintos trámites y escenarios de identificación: https://www.ccss.sa.cr/contacto
- El Tribunal Supremo de Elecciones emitió un nuevo diseño físico de cédula en 2025; su cambio de presentación no justifica reemplazar las reglas del número por la estructura de la tarjeta: https://www.tse.go.cr/comunicado1081.html

**Advertencia:** las reglas sintácticas de este proyecto no comprueban que un documento haya sido emitido, esté vigente ni pertenezca a un paciente.

## Decisiones propuestas (requieren consolidar migración y reglas con PO)

| Código técnico | Documento | Validación preliminar |
|---|---|---|
| CEDULA_NACIONAL | Cédula de identidad nacional | exactamente 9 dígitos ASCII; no todos ceros |
| DIMEX | Documento de identidad migratorio | exactamente 12 dígitos ASCII; no todos ceros |
| TIM | Tarjeta de identificación de menores | solo presencia/longitud general; no inventar formato sin confirmar |
| PASAPORTE | Pasaporte | solo presencia/longitud general; formato depende del país emisor |
| OTRO | Otro documento oficial | solo presencia/longitud general; clasificación y evidencia pendientes |

El componente `Models/Validation/DocumentoPacienteValidator.cs` es una primera pieza reutilizable **sin activar**, para evitar que la UI acepte pasaportes alfanuméricos mientras la base todavía exige identificación numérica.

## Secuencia necesaria para activar la funcionalidad

1. Preparar una migración versionada que añada `Pacientes.TipoDocumento` y adapte la restricción sobre `Numero_Identificacion` sin perder la unicidad ni modificar registros preexistentes de forma especulativa.
2. Conservar los documentos de pacientes históricos sin asumir que todos son cédulas; definir un estado de clasificación pendiente y un procedimiento de regularización.
3. Modificar **en conjunto** el ViewModel, la vista, el repositorio y el procedimiento de registro, además de la edición, para persistir ambos campos de modo atómico.
4. Mantener el alta especializada de recién nacidos: expediente inicial sin identificación, con nacimiento asociado; la asignación posterior debe incluir el tipo documental si la regla definitiva lo exige.
5. Ejecutar pruebas en `Hospital_DB4_Integracion`: cédula nacional, DIMEX, pasaporte alfanumérico, valores repetidos, documentos históricos y recién nacidos. No aplicar a `Hospital_DB4` principal.
6. Validar criterios y actualizar las historias pendientes en UTC Agile.

## Pendientes de producto

- Aprobación de tipos adicionales, incluyendo personas sin documento y los casos excepcionales de atención hospitalaria.
- Regla de unicidad ante documentos de países emisores diferentes.
- Persistencia del país emisor (especialmente pasaportes).
- Permisos y trazabilidad de cambios de identificación.
- Reglas definitivas para fechas, correo y contacto en contextos de emergencia.
