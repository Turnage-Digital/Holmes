namespace Holmes.App.Server.Endpoints;

internal static class AuthPageRenderer
{
    public static string RenderOptionsPage(string destination)
    {
        var encoded = Uri.EscapeDataString(destination);
        return $$"""
                 <!DOCTYPE html>
                 <html lang="en">
                   <head>
                     <meta charset="utf-8" />
                     <title>Holmes Sign In</title>
                     <style>
                       :root {
                         font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                       }
                       body {
                         margin: 0;
                         background: #f5f5f5;
                         min-height: 100vh;
                         display: flex;
                         align-items: center;
                         justify-content: center;
                       }
                       .card {
                         background: #fff;
                         padding: 2.5rem;
                         border-radius: 16px;
                         width: min(400px, 90vw);
                         text-align: center;
                         box-shadow: 0 25px 80px rgba(0,0,0,0.12);
                       }
                       h1 { margin-top: 0; color: #1b2e5f; }
                       p { color: #555; }
                       .btn {
                         display: inline-flex;
                         justify-content: center;
                         align-items: center;
                         padding: 0.85rem 1.2rem;
                         background: #1b2e5f;
                         color: #fff;
                         text-decoration: none;
                         border-radius: 6px;
                         font-weight: 600;
                       }
                       .btn:hover { background: #16244a; }
                     </style>
                   </head>
                   <body>
                     <div class="card">
                       <h1>Sign in to Holmes</h1>
                       <p>Select an identity provider to continue.</p>
                       <a class="btn" href="/auth/login?returnUrl={{encoded}}">Continue with Holmes Identity</a>
                     </div>
                   </body>
                 </html>
                 """;
    }

    public static string RenderAccessDeniedPage(string? reason)
    {
        var (title, message) = reason switch
        {
            "uninvited" => ("Invitation Required",
                "You must be invited to Holmes before you can sign in. Please contact your administrator."),
            "suspended" => ("Account Suspended",
                "Your Holmes account has been suspended. Reach out to your administrator for assistance."),
            _ => ("Access Denied",
                "We could not grant you access to Holmes. Please verify your invitation or contact support.")
        };

        return $$"""
                 <!DOCTYPE html>
                 <html lang="en">
                   <head>
                     <meta charset="utf-8" />
                     <title>{{title}}</title>
                     <style>
                       :root {
                         font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                       }
                       body {
                         margin: 0;
                         background: #f5f5f5;
                         min-height: 100vh;
                         display: flex;
                         align-items: center;
                         justify-content: center;
                         color: #1b2e5f;
                       }
                       .card {
                         background: #fff;
                         padding: 2.5rem;
                         border-radius: 16px;
                         width: min(420px, 90vw);
                         text-align: center;
                         box-shadow: 0 25px 80px rgba(0,0,0,0.12);
                       }
                       h1 { margin-top: 0; }
                       p { color: #555; line-height: 1.5; }
                       a {
                         display: inline-block;
                         margin-top: 2rem;
                         color: #1b2e5f;
                         text-decoration: none;
                         font-weight: 600;
                       }
                     </style>
                   </head>
                   <body>
                     <div class="card">
                       <h1>{{title}}</h1>
                       <p>{{message}}</p>
                       <a href="/auth/options">Return to sign in</a>
                     </div>
                   </body>
                 </html>
                 """;
    }
}
