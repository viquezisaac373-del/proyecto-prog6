# Integración selectiva de HU12 — 22/11/2026

**Origen:** PR #15 (feature/hu12-validaciones-pacientes), basado en una revisión del código de esa rama frente a `develop`.  
**Destino del trabajo rescatado:** PR #22 (`fix/qa-identificaciones-madres-sangre-20261122`), para evitar conflictos con la cédula de 9 dígitos y el módulo de RN.  
**Estado:** integración de código preparada, NO compilada ni validada funcionalmente.

## Portado desde HU12

1. **Correo:** validación más estricta en `PacienteFormViewModel` de puntos consecutivos en el segmento local, dominio completo y formato habitual de email (manteniendo `EmailAddress`). `a@g.com` es válido sintácticamente.
2. **Teléfonos:** `libphonenumber-csharp` 9.0.42 ya formaba parte de `develop` y del PR #22. Se reutiliza en servidor para `NumeroTelefono` y `TelefonoEmergencia` con región CR por defecto, interpretación de números E.164 (`+...`) y validación por país. La vista `Create.cshtml` valida más precisamente con `isValidNumberPrecise()` y muestra un error al abandonar cada campo.
3. **Nombres y apellidos:** exige al menos una letra Unicode; admite letras con tildes, espacios, apóstrofos, guiones y puntos.
4. **Contacto de emergencia:** se aplica el mismo control de contenido de nombre.
5. **Dirección:** rechaza valores que sean solo números o una letra; requiere al menos tres caracteres y contenido alfabético.

## Excluido deliberadamente

- La antigua regla provisional de identificaciones **9–25 dígitos**; el PR #22 exige **9 dígitos para cédulas nacionales en altas nuevas**, preservando los documentos históricos en edición ordinaria.
- El límite de edad **125 años** (sin aprobación del Product Owner).
- La activación de `TipoDocumento`, `DIMEX`, `TIM`, `PASAPORTE`: requeriría persistencia, migración de BD, políticas de identidad y pruebas; el `DocumentoPacienteValidator` del PR #15 es una pieza preparatoria, NO un módulo integrado.
- El reemplazo íntegro del `PacienteFormViewModel.cs` y de la vista del PR #15, porque eliminaría cambios recientes, incluidos `Indeterminado`, restricción de edición de campos sensibles y entrada de cédulas de 9 dígitos.
- Modificar SQL o bases locales. Esta integración de HU12 no requiere migración adicional por sí misma.

## Pruebas que faltan antes de fusionar

| Caso | Acciones | Resultado esperado |
|---|---|---|
| HU12-01 | Alta con nombres tildados, apóstrofos, guiones o espacios | Admite combinaciones válidas |
| HU12-02 | Alta con nombre solo numérico, símbolos inapropiados | Rechaza |
| HU12-03 | `persona@g.com` | Admite sintácticamente |
| HU12-04 | Emails con puntos consecutivos, dominio incompleto | Rechaza |
| HU12-05 | Teléfono costarricense válido (8 dígitos) | Valida en vista y servidor |
| HU12-06 | Teléfono internacional válido en E.164 (`+...`) | Valida, persiste dígitos en SQL |
| HU12-07 | Teléfono sin longitud válida o prefijo inventado | Rechaza |
| HU12-08 | Dirección `123`, `A`, vacío | Rechaza |
| HU12-09 | Dirección descriptiva normal | Admite |
| HU12-10 | Edición de paciente histórico con identificación de más de 9 dígitos | Puede editar contacto sin alterar identificación, fecha, género ni sangre |
| HU12-11 | Alta general con cédula de 9 dígitos, alta con 12 dígitos | Acepta formato de 9; rechaza 12 |
| HU12-12 | Registrar/editar RN y consultar bitácora tras HU12 | No debe producir regresiones |

Pruebas únicamente con datos ficticios en `Hospital_DB4_Integracion`. Verificar tanto mensajes MVC como integridad de datos en SQL y auditoría del UsuarioId.

## Criterio de cierre de HU12

El PR #15 **no debe fusionarse directamente**: continúa divergiendo de `develop`. Una vez verificadas y fusionadas estas correcciones, documentar la integración parcial de HU12 en UTC Agile. El trabajo de tipos documentales y las excepciones clínicas se mantienen en el Product Backlog sin declararlas completas ni eliminarlas silenciosamente.
