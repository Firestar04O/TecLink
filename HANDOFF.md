# Handoff técnico — TecLink

**Corte del documento:** 21 de octubre de 2026  
**Proyecto:** TecLink  
**Estado general:** MVP funcional implementado en una escena Unity, respaldado por Firebase Authentication y Cloud Firestore. La implementación existe en código y está conectada a la escena, pero aún requiere validación integral con dos cuentas reales, reglas desplegadas y un build Android final.

## 1. Resumen ejecutivo

TecLink es una aplicación móvil para conectar estudiantes de TECSUP alrededor de proyectos colaborativos. Solo admite cuentas del dominio `@tecsup.edu.pe` y exige verificación del correo antes de habilitar las funciones principales.

El flujo implementado cubre:

1. Registro e inicio de sesión.
2. Verificación del correo institucional.
3. Creación y edición del perfil.
4. Publicación, edición, consulta y cierre de proyectos.
5. Postulación a proyectos.
6. Revisión y aceptación o rechazo de postulaciones.
7. Creación automática de un chat al aceptar una postulación.
8. Mensajería en tiempo real entre propietario y postulante.

La aplicación usa una sola escena, `Assets/_TecLink/Scenes/TecLink.unity`, con doce pantallas uGUI que se activan y desactivan mediante `NavigationManager`.

## 2. Stack y configuración

| Área | Configuración actual |
|---|---|
| Unity | `6000.3.9f1` |
| Render | Universal Render Pipeline `17.3.0` |
| UI | uGUI `2.0.0` + TextMesh Pro |
| Input | Input System `1.18.0` |
| Backend | Firebase Authentication + Cloud Firestore |
| Firebase Unity SDK | `13.16.0` |
| Firebase project ID | `teclink-84ba2` |
| Plataforma principal | Android, orientación vertical |
| Android package | `com.ricardo.teclink` |
| Android mínimo | API 25 |
| Versión de app | `1.0`, version code `1` |
| MCP | CoplayDev MCP for Unity `v10.3.0` |
| Control de versiones | La copia no es un repositorio Git; contiene metadata de Plastic/Unity Version Control |

La configuración Android de Firebase está en `Assets/google-services.json` y coincide con el application ID. No copiar su contenido a documentación pública. La seguridad efectiva depende de Authentication y de las reglas de Firestore, no de ocultar el archivo de configuración cliente.

## 3. Estructura importante

```text
Assets/_TecLink/
├── Editor/
│   └── Phase2SceneBuilder.cs       # Generador completo de la escena
├── Scenes/
│   └── TecLink.unity               # Escena principal
└── Scripts/
    ├── Core/                        # Estado, navegación y arranque de la app
    ├── Firebase/                    # Authentication y acceso a Firestore
    ├── Models/                      # DTO y resultados de operaciones
    └── UI/                          # Controladores y vistas de las 12 pantallas

Assets/Firebase/                    # SDK Firebase importado
Assets/google-services.json         # Configuración Android Firebase
Packages/manifest.json              # Paquetes Unity, incluido Unity MCP
firestore.rules                     # Reglas locales de Cloud Firestore
firebase.json                       # Apunta a firestore.rules
ProjectSettings/EditorBuildSettings.asset
```

Archivos de entrada recomendados para entender el proyecto:

- `Assets/_TecLink/Scripts/Core/AppManager.cs`
- `Assets/_TecLink/Scripts/Core/NavigationManager.cs`
- `Assets/_TecLink/Scripts/Core/SessionManager.cs`
- `Assets/_TecLink/Scripts/Core/AppConstants.cs`
- `Assets/_TecLink/Scripts/Firebase/AuthService.cs`
- `Assets/_TecLink/Scripts/Firebase/FirestoreService.cs`
- `Assets/_TecLink/Editor/Phase2SceneBuilder.cs`
- `firestore.rules`

## 4. Arquitectura de ejecución

### Inicio

`FirebaseBootstrap` tiene orden de ejecución `-10000`, inicializa Firebase y vive entre escenas. `SessionManager` se inicia después (`-9000`) y conserva el usuario, perfil, proyecto y chat seleccionados. `AppManager` (`-8000`) espera la inicialización de Authentication, restaura la sesión y decide la primera pantalla.

Flujo de entrada:

```text
Firebase listo
  → ¿hay sesión?
    → no: Login
    → sí: refrescar usuario
      → correo no verificado: VerifyEmail
      → correo verificado: cargar users/{uid}
        → no existe: ProfileSetup
        → existe: Feed
```

### Navegación

`NavigationManager` no cambia de escena. Activa exactamente una pantalla de la escena principal:

- `Screen_Login`
- `Screen_Register`
- `Screen_VerifyEmail`
- `Screen_ProfileSetup`
- `Screen_Feed`
- `Screen_CreateProject`
- `Screen_ProjectDetail`
- `Screen_MyProjects`
- `Screen_ApplyProject`
- `Screen_Applications`
- `Screen_Chats`
- `Screen_Chat`

### Servicios

`AuthService` expone registro, login, recarga del usuario, reenvío de verificación y logout. Valida el dominio institucional tanto en cliente como mediante las reglas de Firestore.

`FirestoreService` concentra todas las operaciones de datos:

- cargar y guardar perfil;
- crear, listar, cargar, editar y cerrar proyectos;
- crear y consultar postulaciones;
- aceptar o rechazar postulaciones;
- listar chats;
- escuchar mensajes en tiempo real;
- enviar mensajes mediante escrituras por lote.

Los controladores de UI llaman directamente a estos servicios. No existe una capa de repositorio adicional ni inyección de dependencias; el diseño usa singletons de escena.

## 5. Modelo de datos Firestore

```text
users/{uid}
  uid, email, displayName, career, campus, bio,
  skills[], portfolioUrl, avatarId, createdAt, updatedAt

projects/{projectId}
  ownerId, ownerName, ownerCareer, title, description,
  category, requiredSkills[], tools[], modality, campus,
  hoursPerWeek, deadline?, status, createdAt, updatedAt

projects/{projectId}/interests/{applicantId}
  applicantId, applicantName, message, status,
  chatId?, createdAt, updatedAt

chats/{projectId_applicantId}
  projectId, projectTitle, ownerId, applicantId,
  memberIds[], lastMessage, lastMessageAt, createdAt

chats/{chatId}/messages/{messageId}
  senderId, text, createdAt
```

Estados de proyecto definidos: `open`, `selecting`, `filled`, `closed`.  
Estados de postulación: `pending`, `accepted`, `rejected`.  
Modalidades: `presencial`, `remoto`, `hibrido`.

Las reglas locales restringen el acceso a usuarios autenticados, con correo verificado y dominio `tecsup.edu.pe`. También validan campos, longitudes, propiedad de proyectos, transiciones de estado y membresía de chats. Todo lo no contemplado termina en denegación.

Importante: no hay `.firebaserc`. El proyecto Firebase no queda seleccionado por archivo local. Antes de desplegar reglas, usar explícitamente el proyecto correcto o crear la asociación de forma consciente. El despliegue de las reglas actuales no fue verificado durante este handoff.

## 6. Escena y generador

La escena principal tiene referencias válidas para todos los controladores activos. No se encontraron componentes con script perdido. Los scripts antiguos `FeedPlaceholderController` y `ProfileSetupPlaceholderController` no están conectados a la escena y pueden considerarse candidatos a limpieza después de verificar que no sean necesarios.

### Advertencia crítica

El menú **`TecLink > Construir escena de Fase 6`** ejecuta `Phase2SceneBuilder.BuildScene()`. Ese método crea una escena vacía nueva y guarda el resultado sobre:

`Assets/_TecLink/Scenes/TecLink.unity`

Por tanto, puede reemplazar por completo ajustes manuales hechos en la escena. Antes de ejecutarlo:

1. crear un changeset/checkpoint en Plastic;
2. confirmar si el cambio debe hacerse también en `Phase2SceneBuilder.cs`;
3. reconstruir la escena;
4. revisar el diff de la escena;
5. abrirla y verificar referencias, consola y flujo visual.

El nombre de clase `Phase2SceneBuilder` es histórico; el menú y la escena generada corresponden actualmente a “Fase 6”.

## 7. Estado funcional observado

Implementado y conectado en escena:

- inicialización de Firebase;
- registro, login, logout y verificación de correo;
- creación/edición de perfil;
- feed de proyectos abiertos con filtro local;
- publicación y edición de proyectos;
- lista de proyectos propios;
- detalle y cierre de proyecto;
- postulación con mensaje;
- lista de postulantes;
- aceptación/rechazo;
- lista de chats;
- chat con listener en tiempo real;
- popups de carga, error y confirmación;
- adaptación al safe area móvil.

Última comprobación del MCP, realizada el 20 de octubre de 2026:

- una instancia `TecLink` conectada por HTTP;
- escena activa `Assets/_TecLink/Scenes/TecLink.unity`;
- plataforma del Editor configurada como Android;
- Editor sin compilación pendiente y listo para herramientas.

Esta conexión debe volver a comprobarse al retomar el trabajo; no debe asumirse que una sesión MCP anterior sigue viva.

## 8. Pendientes y riesgos conocidos

### Prioridad alta

1. **Pruebas end-to-end:** no hay pruebas EditMode ni PlayMode del proyecto. Hace falta validar el ciclo completo con dos cuentas institucionales verificadas: propietario y postulante.
2. **Reglas desplegadas:** existe una implementación local robusta, pero no se confirmó que sea la versión publicada en Firebase.
3. **Build Android real:** no se verificó un APK/AAB instalado en dispositivo. Probar login, enlaces de verificación, Firestore, teclado, safe area y reconexión.
4. **Estado `filled`:** está definido y representado en UI/reglas, pero no hay una operación del servicio que lleve un proyecto a `filled`. Definir cuándo ocurre.
5. **Aceptaciones múltiples:** el código permite potencialmente aceptar más de una postulación mientras el proyecto está en `selecting`. Confirmar si es una función de equipos o si debe limitarse a una persona.

### Prioridad media

6. `Assets/Scenes/SampleScene.unity` sigue habilitada como segunda escena del build. Eliminarla del build si no se usa.
7. `companyName` continúa como `DefaultCompany`.
8. No hay README previo, estrategia de ambientes, documentación de despliegue ni CI.
9. Las consultas tienen límites fijos (`50` proyectos, `50` postulaciones, `50` chats y `100` mensajes) sin paginación de UI.
10. El filtrado del feed ocurre después de descargar hasta 50 proyectos; no escala a un catálogo grande.
11. La escena es un archivo grande y monolítico, generado mayormente por código; aumenta el riesgo de conflictos y de pérdida de cambios manuales.
12. Hay logs recientes de procesos de compilación de shaders desconectados. No se encontraron errores C# en los logs inspeccionados, pero se debe revisar la consola real antes de cada entrega.

### Seguridad y operación

13. No relajar las reglas de Firestore para solucionar errores de permisos. Primero comprobar correo verificado, proyecto Firebase seleccionado y versión desplegada.
14. Mantener el MCP en transporte local. Si se expone remotamente, exigir API key y limitar el acceso.
15. Antes de acciones MCP masivas sobre escenas, prefabs o paquetes, crear un checkpoint y usar operaciones pequeñas verificables.

## 9. Build Settings actuales

Orden de escenas:

1. `Assets/_TecLink/Scenes/TecLink.unity`
2. `Assets/Scenes/SampleScene.unity`

`TecLink.unity` es la escena de arranque correcta. La segunda escena parece residual del template.

## 10. Secuencia recomendada para continuar

1. Abrir el proyecto con Unity `6000.3.9f1` y esperar a que termine la importación.
2. Revisar la consola y confirmar que no hay errores de compilación.
3. Abrir `TecLink.unity`; no reconstruirla todavía.
4. Verificar el estado de Plastic y crear un checkpoint.
5. Confirmar en Firebase Authentication que Email/Password está habilitado.
6. Confirmar que las reglas locales coinciden con las desplegadas.
7. Ejecutar el flujo con dos cuentas `@tecsup.edu.pe`:
   - registrar y verificar ambas;
   - completar perfiles;
   - publicar proyecto con la cuenta A;
   - postular con la cuenta B;
   - aceptar con A;
   - enviar mensajes en ambos sentidos;
   - cerrar el proyecto.
8. Corregir primero cualquier fallo de consistencia entre cliente y reglas.
9. Definir la semántica de `selecting` y `filled`.
10. Añadir pruebas mínimas para validadores, conversiones y reglas críticas.
11. Retirar `SampleScene` del build y actualizar metadatos de producto.
12. Generar e instalar un build Android de prueba.

## 11. Uso seguro de MCP for Unity

El paquete está fijado a `v10.3.0` en `Packages/manifest.json`. Al trabajar mediante MCP:

1. comprobar primero el estado del Editor y la instancia activa;
2. leer jerarquía/componentes antes de modificar objetos;
3. hacer cambios repetitivos mediante lotes pequeños;
4. después de editar scripts, esperar la compilación y revisar errores;
5. después de cambios visuales, tomar una captura de Game View o Scene View;
6. después de cambios importantes, guardar la escena y revisar el changeset;
7. no ejecutar el generador completo salvo que se quiera regenerar toda la UI.

## 12. Criterio mínimo de entrega del MVP

El MVP debería considerarse entregable cuando:

- compila sin errores en Editor;
- las reglas correctas están desplegadas;
- el recorrido de dos usuarios funciona de extremo a extremo;
- la mensajería se actualiza en tiempo real;
- cerrar sesión y restaurar sesión funcionan;
- el build Android se instala y ejecuta en dispositivo;
- no se incluye `SampleScene` innecesariamente;
- existe un changeset identificable para la versión entregada.

