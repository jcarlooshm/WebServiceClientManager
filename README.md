# WebServiceClientManager

La librería `WebServiceClientManager` proporciona una interfaz para realizar peticiones HTTP de manera sencilla a servicios web, facilitando la gestión de tokens de autorización y ofreciendo métodos para realizar operaciones comunes como GET, POST, PUT, DELETE y PATCH.

## Instalación

Para instalar la librería, puedes utilizar el siguiente comando en tu terminal:

```bash
dotnet add package WebServiceClientManager
```

## Clase Post
La clase Post representa un objeto que podría ser utilizado en operaciones como POST para enviar datos al servidor. En este contexto, estaremos trabajando con el API de pruebas https://jsonplaceholder.typicode.com y la clase Post estará alineada con la estructura de datos proporcionada por ese API.

```bash
public class Post 
{
    public int userId { get; set; }
    public int id { get; set; }
    public string title { get; set; }
    public string body { get; set; }
}
```

## Uso Básico - Sincrónico
### GET para un solo registro
```bash
// Instanciación del Cliente
var webServiceClient = new WebServiceClient("https://jsonplaceholder.typicode.com");

// Realizar una Petición GET para un solo registro
var response = webServiceClient.Get<Post>("/posts/1");

if (response.IsSuccess)
{
    // Accede a la respuesta exitosa
    var responseData = response.Content;
    // Haz algo con los datos
}
```

### GET para una lista de registros
```bash
// Instanciación del Cliente
var webServiceClient = new WebServiceClient("https://jsonplaceholder.typicode.com");

// Realizar una Petición GET para una lista de registros
var response = webServiceClient.Get<List<Post>>("/posts");

if (response.IsSuccess)
{
    // Accede a la respuesta exitosa
    var responseData = response.Content;
    // Haz algo con los datos
}
```

### POST
```bash
/// Instanciación del Cliente
var webServiceClient = new WebServiceClient("https://jsonplaceholder.typicode.com");

// Datos a enviar
var postData = new Post { userId = 1, title = "Nuevo Post", body = "Contenido del post" };

// Realizar una Petición POST
var response = webServiceClient.Post<Post>("/posts", postData, EContentType.application_json);

if (response.IsSuccess)
{
    // Accede a la respuesta exitosa
    var responseData = response.Content;
    // Haz algo con los datos
}
```


## Uso Básico - Asincrónico
### GET para un solo registro
```bash
// Instanciación del Cliente
var webServiceClient = new WebServiceClient("https://jsonplaceholder.typicode.com");

// Realizar una Petición GET Asincrónica para un solo registro
var response = await webServiceClient.GetAsync<Post>("/posts/1");

if (response.IsSuccess)
{
    // Accede a la respuesta exitosa
    var responseData = response.Content;
    // Haz algo con los datos
}
```

### GET para una lista de registros
```bash
// Instanciación del Cliente
var webServiceClient = new WebServiceClient("https://jsonplaceholder.typicode.com");

// Realizar una Petición GET Asincrónica para una lista de registros
var response = await webServiceClient.GetAsync<List<Post>>("/posts");

if (response.IsSuccess)
{
    // Accede a la respuesta exitosa
    var responseData = response.Content;
    // Haz algo con los datos
}
```

### POST
```bash
// Instanciación del Cliente
var webServiceClient = new WebServiceClient("https://jsonplaceholder.typicode.com");

// Datos a enviar
var postData = new Post { userId = 1, title = "Nuevo Post", body = "Contenido del post" };

// Realizar una Petición POST Asincrónica
var response = await webServiceClient.PostAsync<Post>("/posts", postData, EContentType.application_json);

if (response.IsSuccess)
{
    // Accede a la respuesta exitosa
    var responseData = response.Content;
    // Haz algo con los datos
}
```

## Uso de Bearer Token
La librería WebServiceClientManager facilita la inclusión de tokens Bearer en las peticiones HTTP. Para utilizar un token Bearer, simplemente sigue estos pasos:

1. **Instancia del Cliente:** Crea una instancia de WebServiceClient con la URL base del servicio.
2. **Establecer el Token Bearer:** Antes de realizar cualquier petición, utiliza el método SetAuthorizationToken para establecer el token Bearer. Este método configura automáticamente el encabezado de autorización en las solicitudes subsiguientes.
```bash
var webServiceClient = new WebServiceClient("https://jsonplaceholder.typicode.com");
var bearerToken = "tu_token_de_acceso";  // Reemplaza con tu propio token
webServiceClient.SetAuthorizationToken(bearerToken);
```

3. **Realizar la Petición:** Luego de establecer el token Bearer, puedes realizar tus solicitudes HTTP de manera normal. El token se incluirá automáticamente en el encabezado de autorización de cada solicitud.
```bash
var response = webServiceClient.Get<Post>("/posts/1");
```

## Autenticación y uso del Token
### Realizar un Login y obtener un token

```bash
// Instanciación del Cliente para la autenticación
var authClient = new WebServiceClient("https://tu-servicio-auth.com");

// Datos de login
var loginData = new
{
    username = "tu_usuario",
    password = "tu_contraseña"
};

// Realizar una petición POST para el login
var authResponse = authClient.Post<dynamic>("/login", loginData, EContentType.application_json);

if (authResponse.IsSuccess)
{
    // Obtener el token de la respuesta
    var accessToken = authResponse.Content.access_token;

    // Instanciación del Cliente principal o reutilización de la instancia existente con el token obtenido
    var webServiceClient = new WebServiceClient("https://tu-servicio-api.com");
    webServiceClient.SetAuthorizationToken(accessToken);

    // Ahora puedes usar el mismo cliente para realizar otras solicitudes con el token de acceso
    var response = webServiceClient.Get<Post>("/posts/1");

    if (response.IsSuccess)
    {
        // Accede a la respuesta exitosa
        var responseData = response.Content;
        // Haz algo con los datos
    }
    else
    {
        // Manejar error en la solicitud subsiguiente
    }
}
else
{
    // Manejar error en la autenticación
}
```

En este ejemplo, se ha agregado un comentario para resaltar la reutilización de la instancia del cliente (webServiceClient). Este enfoque puede mejorar la eficiencia y el rendimiento de tu aplicación al evitar la creación innecesaria de nuevas instancias del cliente al hacer solicitudes subsiguientes al mismo API.


## Métodos adicionales
- Put, Delete, Patch: Métodos para realizar operaciones PUT, DELETE y PATCH respectivamente.
- Métodos Async: Versiones asincrónicas de los métodos mencionados para operaciones asíncronas.
- GenerateQueryParamsFromObject: Genera una cadena de consulta a partir de un objeto.
