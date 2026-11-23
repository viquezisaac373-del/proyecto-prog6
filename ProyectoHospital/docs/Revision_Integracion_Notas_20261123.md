# Hospinomenal — conciliación de observaciones de equipo (23/11/2026)

Estado: revisión de código de la rama acumulativa `feat/proveedores-abastecimientos-20261123`.
No sustituye la aprobación del PO ni pruebas de regresión. Las correcciones que Danna
está reconsiderando **no se modifican** hasta recibir sus capturas.

## PR y dependencias

- #24: notificaciones de RN en bitácora, Draft y base `develop`.
- #25: usuarios/empleados/médicos/clientes, Draft y base `develop`.
- #26: disponibilidad médica semanal, Draft sobre #25.
- #27: recomendaciones internas de reposo, Draft sobre #26.
- #28: proveedores/abastecimientos, Draft sobre #27.

El orden de integración de las ramas apiladas es #25 → #26 → #27 → #28,
con pruebas de regresión y revisión de migraciones. #24 se integra aparte
tras comprobar compatibilidad con la rama acumulativa.
No afirmar integración en `develop` mientras continúen abiertos.

## Observaciones de reunión: hechos del código vs. pendientes

| Nota | Situación en código examinado | Acción |
|---|---|---|
| Bitácora de RN | Implementada en #24; probada previamente en integración por el equipo | Revalidar tras fusionar |
| Usuarios, médicos, clientes | Implementados en #25 y probados en integración | Mantener, probar regresión |
| Sexo del RN corregible | Formulario de edición muestra valor de solo lectura | Esperar imágenes y criterio de corrección excepcional |
| Tipo de parto no editable | `Views/RecienNacidos/Editar.cshtml` mantiene `<select asp-for="TipoParto">` | Pendiente de validar con equipo clínico/PO |
| Grupo sanguíneo posterior | Existe `CompletarTipoSangre` separado | Ya cubierto, confirmar QA |
| Semanas de gestación | `Range(1,255)`; demasiado amplio sin validación de plausibilidad | Definir límites clínicos y tratamiento de excepciones |
| Peso/talla digitables | `input type=number` ya existe en alta y edición | No cambiar hasta aclaración de Danna |
| Respiración obligatoria | `Respiracion` está definido opcional | Esperar confirmación; no inventar evaluación clínica |
| Hallazgos presentes/ausentes | Campo `Malformaciones` es texto libre | Requiere especificar semántica de hallazgo negado vs. no evaluado |
| Revisión de obligatorios | Hay Required en identidad/contacto/fecha/tipo de parto; clínicos son en gran parte opcionales | Revisión con imágenes/criterios del PO |
| Teléfonos transversales | Hay validadores específicos en RN; no se confirma homogeneidad en todos los módulos | Auditoría transversal pendiente |
| NULL | Distintos módulos permiten nulos cuando el dato no está disponible | No reemplazar desconocido por dato inventado; revisar campo por campo |
| Bitácoras, estabilidad | Hay registro de auditoría; no equivale a resistencia total a fallos | Probar manejo de errores y regresión |
| Panel Citas/Medicamentos | Los controladores son placeholders, pero se ofrecen como tarjetas | Corregido en la rama acumulativa: no anunciar módulos incompletos |
| Requerimientos adicionales PO | Horarios #26, reposo interno #27 y proveedores #28 implementados en rama | No llamar oficial a la recomendación; pruebas negativas pendientes |

## QA recomendada antes de review

1. Compilar rama acumulativa y abrir `/Modulos` como admin y como médico.
2. Admin ve Pacientes, RN, Horarios, Usuarios, Proveedores, Bitácoras; NO Citas/Medicamentos.
3. Médico ve RN, Horarios, Recomendaciones; NO tarjetas de admin.
4. Reconfirmar que los vínculos funcionan y sus permisos del servidor no cambian.
5. Hacer una sola regresión con cada flujo importante: paciente/RN, alta de médico, cliente,
   horarios, recomendación, proveedor, bitácoras.
6. Revisar imágenes nuevas de Danna y acordar criterios antes de modificar campos
   clínicos o límites relacionados con gestación/neonatología.
7. No fusionar a develop sin respaldos/migraciones coordinadas con responsable SQL.

## Advertencias

- El panel no sustituye autorización del servidor; ésta sigue en controladores.
- Registro de recomendaciones privadas != incapacidad homologada por CCSS/INS.
- Mostrar cupos teóricos de horarios no equivale a agenda de citas confirmada.
- Movimiento de abastecimiento no equivale a conciliación física de inventario.
- No se promete tolerancia a fallos absoluta.
