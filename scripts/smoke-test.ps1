param(
    [string]$ApiBaseUrl = "http://localhost:5000/api"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Api {
    param(
        [Parameter(Mandatory)][ValidateSet("GET", "POST", "PUT", "DELETE")]
        [string]$Method,
        [Parameter(Mandatory)]
        [string]$Path,
        [hashtable]$Headers = @{},
        [object]$Body
    )

    $arguments = @{
        Method      = $Method
        Uri         = "$ApiBaseUrl$Path"
        Headers     = $Headers
        ContentType = "application/json; charset=utf-8"
    }
    if ($null -ne $Body) {
        $arguments.Body = $Body | ConvertTo-Json -Depth 10
    }

    try {
        Invoke-RestMethod @arguments
    }
    catch {
        $responseBody = $_.ErrorDetails.Message
        throw "API $Method $Path failed. $responseBody"
    }
}

function Login {
    param(
        [Parameter(Mandatory)][string]$Username,
        [Parameter(Mandatory)][string]$Password
    )

    $result = Invoke-Api -Method POST -Path "/auth/login" -Body @{
        username = $Username
        password = $Password
    }
    if ([string]::IsNullOrWhiteSpace($result.token)) {
        throw "Login $Username did not return a JWT."
    }
    @{
        Authorization = "Bearer $($result.token)"
    }
}

Write-Host "Logging in with all four roles..."
$adminHeaders = Login -Username "admin" -Password "Admin@123"
$salesHeaders = Login -Username "sales" -Password "Sales@123"
$warehouseHeaders = Login -Username "warehouse" -Password "Warehouse@123"
$deliveryHeaders = Login -Username "delivery" -Password "Delivery@123"

Write-Host "Selecting a customer and an in-stock product..."
$customers = Invoke-Api -Method GET -Path "/customers?page=1&pageSize=100" -Headers $salesHeaders
$products = Invoke-Api -Method GET -Path "/products?page=1&pageSize=100" -Headers $salesHeaders
$customer = $customers.items | Select-Object -First 1
$product = $products.items | Where-Object { $_.stockQuantity -ge 1 } | Select-Object -First 1
if ($null -eq $customer -or $null -eq $product) {
    throw "Seed data must contain a customer and an in-stock product."
}

Write-Host "Sales creates and confirms an order..."
$order = Invoke-Api -Method POST -Path "/orders" -Headers $salesHeaders -Body @{
    customerId          = $customer.id
    expectedDeliveryDate = (Get-Date).AddDays(1).ToString("o")
    deliveryAddress     = $customer.address
    paymentMethod       = "CashOnDelivery"
    note                = "Order created by scripts/smoke-test.ps1"
    items               = @(
        @{
            productId = $product.id
            quantity  = 1
        }
    )
}
$order = Invoke-Api -Method POST -Path "/orders/$($order.id)/confirm" -Headers $salesHeaders
if ($order.orderStatus -ne "Confirmed") {
    throw "Order did not transition to Confirmed."
}

Write-Host "Warehouse prepares and marks the order ready..."
$order = Invoke-Api -Method POST -Path "/orders/$($order.id)/prepare" -Headers $warehouseHeaders
if ($order.orderStatus -ne "Preparing") {
    throw "Order did not transition to Preparing."
}
$order = Invoke-Api -Method POST -Path "/orders/$($order.id)/ready" -Headers $warehouseHeaders
if ($order.orderStatus -ne "ReadyForDelivery") {
    throw "Order did not transition to ReadyForDelivery."
}

Write-Host "Sales assigns the delivery employee..."
$deliveryStaff = Invoke-Api -Method GET -Path "/users/delivery-staff" -Headers $salesHeaders
$driver = $deliveryStaff | Where-Object { $_.username -eq "delivery" } | Select-Object -First 1
if ($null -eq $driver) {
    throw "The active delivery account was not found."
}
$order = Invoke-Api -Method POST -Path "/orders/$($order.id)/assign-delivery" `
    -Headers $salesHeaders -Body @{ deliveryUserId = $driver.id }

Write-Host "Delivery starts and completes the shipment..."
$myDeliveries = Invoke-Api -Method GET `
    -Path "/deliveries/my-orders?page=1&pageSize=100" -Headers $deliveryHeaders
$delivery = $myDeliveries.items |
    Where-Object { $_.orderId -eq $order.id } |
    Select-Object -First 1
if ($null -eq $delivery) {
    throw "The delivery employee cannot see the newly assigned order."
}
$delivery = Invoke-Api -Method POST -Path "/deliveries/$($delivery.id)/start" `
    -Headers $deliveryHeaders
$delivery = Invoke-Api -Method POST -Path "/deliveries/$($delivery.id)/complete" `
    -Headers $deliveryHeaders -Body @{
        markAsPaid = $true
        note       = "Smoke test completed delivery"
    }
if ($delivery.status -ne "Completed" -or $delivery.order.orderStatus -ne "Delivered") {
    throw "Delivery did not finish in the expected state."
}

Write-Host "Admin reads the dashboard..."
$summary = Invoke-Api -Method GET -Path "/dashboard/summary" -Headers $adminHeaders
if ($summary.deliveredOrders -lt 1) {
    throw "Dashboard did not count the delivered order."
}

Write-Host "SMOKE TEST PASSED - Order $($order.orderCode)" -ForegroundColor Green
