# ContactLandingApi

Backend en **.NET 8 Minimal API** con Docker, MySQL, JWT y una vista monitor en `/`.
Está preparado para desplegarse en **Railway** conectado a un servicio **MySQL** del mismo proyecto.

## Qué hace

- Guarda contactos en MySQL
- Genera un código único por contacto
- Evita correos duplicados
- Expone API pública para registrar contactos
- Expone API privada con JWT
- Muestra un monitor en `/` con los registros guardados
- Incluye `Dockerfile` y `railway.json`

## Variables que necesita el backend

Puedes usar solo estas en Railway:

- `MYSQL_URL`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Secret`
- `Admin__Username`
- `Admin__PasswordHash`
- `Captcha__Provider`
- `Captcha__SecretKey`

Hay un ejemplo listo en `.env.railway.example`.

## Paso a paso en Railway

### 1. Sube este proyecto a GitHub

Sube la carpeta completa a un repositorio.

### 2. Crea el servicio backend en Railway

En el mismo proyecto donde ya tienes MySQL:

1. Pulsa **New**
2. Elige **GitHub Repo**
3. Selecciona este repositorio
4. Railway detectará el `Dockerfile`
5. Espera el primer deploy

### 3. Configura las variables del backend

En el servicio del backend entra a **Variables** y crea estas:

#### Variable de base de datos

La más importante es esta:

```env
MYSQL_URL=${{MySQL.MYSQL_URL}}
```

`MySQL` debe ser el nombre del servicio de base de datos en Railway. Si tu servicio tiene otro nombre, usa ese.

#### Variables JWT

```env
Jwt__Issuer=ContactLandingApi
Jwt__Audience=ContactLandingApiUsers
Jwt__Secret=CAMBIA_ESTE_SECRETO_POR_UNO_MUY_LARGO_Y_ALEATORIO
```

#### Usuario admin

```env
Admin__Username=admin
Admin__PasswordHash=PEGA_AQUI_EL_HASH_BCRYPT
```

#### Captcha

Para probar:

```env
Captcha__Provider=None
Captcha__SecretKey=
```

Para producción luego puedes usar Turnstile.

### 4. Redeploy

Después de guardar variables, haz **Redeploy** del backend.

### 5. Comprueba que conecta con MySQL

Cuando abra bien:

- `/health` responde OK
- `/` muestra el monitor
- el monitor lista registros de `contacts`

## Cómo generar el hash BCrypt del admin

### Opción rápida con una web o herramienta local

Necesitas el hash BCrypt de la contraseña que usarás para el login.

### Opción en C#

```csharp
BCrypt.Net.BCrypt.HashPassword("TuPasswordSegura123!")
```

Pega el resultado en `Admin__PasswordHash`.

## Endpoints

### Públicos

- `GET /health`
- `GET /api/monitor/contacts`
- `GET /api/contacts/check-email?correo=test@email.com`
- `POST /api/contacts`
- `POST /api/auth/login`

### Privados con JWT

- `GET /api/contacts`
- `PATCH /api/contacts/{id}/use-code`

## JSON para crear contacto

```json
{
  "nombre": "Juan",
  "apellidos": "Pérez",
  "telefono": "600123123",
  "correo": "juan@email.com",
  "empresa": "Mi Empresa",
  "captchaToken": "TOKEN_DEL_CAPTCHA"
}
```

## Login admin

```json
{
  "username": "admin",
  "password": "TU_PASSWORD"
}
```

## Nota sobre la base de datos

- Si Railway entrega `MYSQL_URL` en formato `mysql://...`, la app la convierte automáticamente al formato que necesita EF Core.
- La tabla `contacts` se crea automáticamente al arrancar si no existe.

## Desarrollo local

Puedes usar `appsettings.json` para desarrollo local, pero **no lo uses con secretos reales en producción**.

## Archivos importantes

- `Program.cs`
- `Data/AppDbContext.cs`
- `Services/DbInitializerService.cs`
- `Services/JwtTokenService.cs`
- `Dockerfile`
- `railway.json`
- `wwwroot/index.html`
