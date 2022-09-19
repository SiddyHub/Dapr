# Refactor with Dapr Building Blocks
This repository is intended to cover majority of the fundamental building blocks of Dapr. The idea is to refactor the code written in ASP.NET Core 3.1 in main branch and daprize it from scratch, so that we can reduce our code base by focusing on core business logic and leverage Dapr for integrations to Azure Services.

This is part 1 of DAPR Series. Subsequent series and topics can be found below, to cover more features of Dapr.
1. [Polyglot Persistence with Dapr](https://github.com/SiddyHub/DaprDataManagement)
2. [Using Azure Ad Identity](https://github.com/SiddyHub/DaprAzureAdIdentity)

*Will keep updating the above list with new topics and new code base.

The [main](https://github.com/SiddyHub/Dapr/tree/main) branch, is the actual code base,
and [eshop_daprized](https://github.com/SiddyHub/Dapr/tree/eshop_daprized) branch is the refactored code base with Dapr building blocks.

This version of the code uses **Dapr 1.7**

## Pre-Requisites to Run the Application

- VS Code
  - With [Dapr Extension](https://docs.dapr.io/developing-applications/ides/vscode/vscode-dapr-extension/)
- .NET Core 3.1 SDK
- Docker installed (e.g. Docker Desktop for Windows)
- [Dapr CLI installed](https://docs.dapr.io/getting-started/install-dapr-cli/)
- Access to an Azure subscription (Optional)

## Architecture Overview

![architecture_overview](https://user-images.githubusercontent.com/84964657/191068659-62575c1a-9b42-4849-96d9-6d60c85db505.png)

1. This is a GloboTicket MVC Application which has a catalog service, which interacts with the shopping basket service, when user enters items in basket. 
2. The Shoping basket interacts with the Discount service, to check if any valid coupon code has been enetered as part of the Checkout Process.
3. Once user checks out, the Shopping basket will place an event in queue (Azure Service Bus or Redis).
4. The Order Service picks up this event and create a new Order.
5. The Order service places a new event in queue for another service i.e. Payment.
6. The Payment Service talks to an External Payment Provider Service.
7. On Getting response from the external payment service, the Payment service, places another message on the queue, which will agin be picked up by Order service.
8. The Marketing service will periodically keep checking for new events like User Basket changes etc., and add entry in database.

Overview with the Dapr sidecar running:

![service_invocation](https://user-images.githubusercontent.com/84964657/190984347-bc4d830b-5d20-4ebf-b0c3-08f563d0442f.png)

## Running the app locally

   Our Microservices i.e. Discount, Event Catalog, Shopping Basket, Ordering and Marketing have underlying data stores, which need to be created first. There are migrations folder under each service project which needs to be executed to create the databases.
   So Run Database Migrations for each service project, and once all data stores are created, verify it in SQL Server Management Tools.
   
   Once VS Code with [Dapr Extension](https://docs.dapr.io/developing-applications/ides/vscode/vscode-dapr-extension/) has been installed, we can leverage it to scaffold the configuration for us, instead of manually configuring **launch.json**.

   A **tasks.json** file also gets prepared by the Dapr extension task.

   Follow [this link](https://docs.dapr.io/developing-applications/ides/vscode/vscode-how-to-debug-multiple-dapr-apps/#prerequisites) to know more about configuring `launch.json and tasks.json`

   In VS Code go to Run and Debug, and Run All Projects at the same time or Individual project.

![callstack](https://user-images.githubusercontent.com/84964657/190982330-5724fbae-2caa-49ec-a87a-db425db661c5.jpg)  ![debug](https://user-images.githubusercontent.com/84964657/190982955-b0a69850-4795-444a-aaf3-e2d6120dc1b2.jpg)

   Once the application and side car is running, we can also apply breakpoint to debug the code. Check [this link](https://code.visualstudio.com/docs/editor/debugging#_breakpoints) for more info.

   The Darp extension added also provides information about the applications running and the corresponding components loaded for that application.

   ![dapr_extension_components](https://user-images.githubusercontent.com/84964657/190985678-5b7d24c8-095d-43e5-86fe-0002a5d985ee.png)

## Dapr Building Blocks Covered

**1. Service Invocation**

   Refer [this link](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/#service-invocation-diagram) to know more about Service Invocation works.

   We are going to use Dapr SDK for Service Invocation, by referencing `Dap.AspNetCore` Nuget package.
   In Frontend (GloboTicket.Web) Startup.cs file, under ConfigureServices call `services.AddDaprClient();`, which registers Dapr Client into the Inversion Of Control (IoC) container so it can be easiy used in any of our Services and Controllers.
   
   For our backend services we register our EventCatalog and Order service as a Singleton and Invokes HTTP services using HttpClient by passing inside constructor an HTTP Client been created with `DaprClient.CreateInvokeHttpClient("appid")`. This creates HTTP Client that's already set up for us with correct base address.

   Our ShoppingBasket and Discount services are registered as Scoped, and Invokes HTTP services using DaprClient.

   For ASP.NET Core controllers, the process for setting up the Dapr support inside an ASP.NET Core project:
   
   Firstly, install the `Dapr.AspNetCore` NuGet package.
 
   Then in the `ConfigureServices` method of the Startup class of your ASP.NET Core project, add the following line to register Dapr within controllers, add an instance of the DaprClient, and configure some model binders. Optionally, you can set an additional configuration for the Dapr client by using the builder pattern inside AddDapr:
   ```
   public void ConfigureServices(IServiceCollection services)
   {
      services.AddControllers().AddDapr();
   }
   ```

   The Discount API, implements **GRPC Protocol**. 
   - To implement a gRPC service to run side by side with Dapr, we have to implement the **AppCallback** interface.
     With Dapr .NET SDK, the base class for gRPC comes pre-packaged, so no need to import the Protobuf files and utilize the Protobuf code generation so that it materializes to a C# class.

   - The base class `AppCallback.AppCallbackBase` that our Discount gRPC service must extend has three methods that have to be overriden – `OnInvoke, ListTopicSubscriptions, and OnTopicEvent`.
     
     The `OnInvoke` method gets called whenever a method has been invoked via the Service Invocation building block. In contrast with HTTP-based services, there is a single method that accepts all method invocations. You will typically have a switch-case statement for each of the methods your service supports.

   - Also a Protobuf file need to be created for Request / Response message variable, and add it in the project using "Connected Services" with Client or Server option respectively.

   - Lastly, we need to set `app-ssl` to true, for Discount service Dapr sidecar

**2. Publish and Subscribe**

   Refer [this link](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/#pubsub-api-in-dapr) to know more about how Publish & Subscribe works.
   
   For our broker service we would be using Azure Service Bus (If Azure Subscription is not available one can use Redis Cache container spun up by Dapr).

   - Receiving messages with topic subscriptions
     Dapr applications can subscribe to published topics via two methods that support the same features: declarative and programmatic.

     In Our code we would be using the `programmatic approach`.
     Refer [this link](https://docs.dapr.io/developing-applications/building-blocks/pubsub/subscription-methods/#pubsub-api-subscription-methods) to know more.

   - For our PubSub component definition we would be using Azure Service Bus message broker.
     Refer [this link](https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-azure-servicebus/#component-format) for complete component format metadata fields for Azure Service Bus.

   - Code Configurations to be done
     Inside `Startup.cs Configure` method, add following code lines
     ```
     app.UseCloudEvents();            

     app.UseEndpoints(endpoints =>
     {
        endpoints.MapSubscribeHandler();
        endpoints.MapControllers();
     });     
     ```
     To Publish messages on Dapr:

     After we configure each ASP.NET Web API project to support subscriber handlers and process CloudEvents, we are ready to publish our first message via the Dapr      ASP.NET SDK.

     `await daprClient.PublishEventAsync("pubsub", "checkoutmessage", basketCheckoutMessage);`
   
     To Subscribe to a Dapr Topic:  
     ASP.NET controller method signature can be decorated with the [Topic] attribute to subscribe to messages from the topic
     ```
     [HttpPost("submitorder")]
     [Topic("pubsub", "checkoutmessage")]
     public async Task<IActionResult> Submit(BasketCheckoutMessage basketCheckoutMessage)
     {
        ...ommited...
     }
     ```

**3. Resource Bindings**
   
   - For Dapr Output Binding to send mail via SMTP.
     (Refer [this link](https://docs.dapr.io/reference/components-reference/supported-bindings/smtp/#component-format) for complete component format metadata fields)

     We would be using an Open Source component called `MailDev`. As a pre-requisite, make sure to start maildev locally by using the docker run command `$ docker run -p 1080:1080. -p 1025:1025 maildev/maildev`
     Refer [this link](https://github.com/maildev/maildev) for more info.
   
     Code Changes:

     We would be sending an email once the Order has been successfully placed after Checkout, and added in the Repository.
     When sending an email, the metadata in the configuration and in the request is combined. The combined set of metadata must contain at least the `emailFrom`, `emailTo` and `subject` fields.

     Example:
     ```
     var metadata = new Dictionary<string, string>
     {
        ["emailFrom"] = "noreply@globoticket.shop",
        ["emailTo"] = order.Email,
        ["subject"] = $"Thank you for your order"
     };
     var body = $"<h2>Your order has been received</h2>"
                + "<p>Your tickets are on the way!</p>";
     await daprClient.InvokeBindingAsync("sendmail", "create",
                body, metadata);
     ```

   - For Dapr Cron Input Binding.
     (Refer [this link](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/#component-format) for complete component format metadata fields)

     In Our code the Cron Job would be used by the `Marketing` service every 1m, to check for any Shopping Basket event changes (adding or removing items)

     A cron binding adopts the following configuration:
     ```
     apiVersion: dapr.io/v1alpha1
     kind: Component
     metadata:
       name: scheduled
       namespace: default
     spec:
       type: bindings.cron
       version: v1
       metadata:
       - name: schedule
         value: "@every 1m" # valid cron schedule
     scopes:
     - marketing
     ```
     The relevant settings for the configuration of this component of type bindings.cron are `name and schedule`. With an input binding, the configured name will be used by the Dapr runtime to invoke, as **POST**, the corresponding route at the application endpoint, with the frequency defined in the schedule.
     
     Testing the cron binding
     From an ASP.NET perspective, we need to implement a method with the `scheduled` route in the ASP.NET controller
     ```
     [HttpPost("", Name = "Scheduled")]
     public async void OnSchedule()
     {
        ...ommited...
     }
     ```

**4. Secrets**

   In our code example we would be using local secret store to reference our secret values.
   Refer [this link](https://github.com/dapr/quickstarts/blob/master/secrets_management/components/local-secret-store.yaml) to know how secret management works behind the scene.

**5. Monitoring and Observability**

   Dapr uses Zipkin protocol for distributed traces and metric collection. This is enabled with a Dapr Configuration file.
   After we have run our services via VS Code, go to `http://localhost:9411` , click `Run Query` button to view trace logs.
   
   ![zipkin](https://user-images.githubusercontent.com/84964657/190981484-591e2939-7181-4896-8007-835c7bf8d50d.jpg)

   Another great feature we can use with Observability is Dapr dashboard, a web base UI.
   Refer [this link](https://github.com/dapr/dashboard) to know more about its capability and how to launch the dashboard.

## Troubleshooting notes

- If not able to load Dapr projects when running from VS Code, check if Docker Engine is running, so that it can load all components.
- If using Azure Service Bus as a Pub Sub Message broker make sure to enter primary connection string in `secrets.json`
- If mail binding is not working, make sure `maildev`image is running. Refer [this link](https://github.com/maildev/maildev) for more info.
- For any more service issues, we can check Zipkin trace logs.
