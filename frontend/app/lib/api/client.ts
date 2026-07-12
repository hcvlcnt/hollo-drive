const API_BASE_URL = "/api"

type ApiRequestOptions = Omit<RequestInit, "body"> & {
  body?: unknown
}

export class ApiError extends Error {
  status: number
  data: unknown

  constructor(message: string, status: number, data: unknown) {
    super(message)
    this.name = "ApiError"
    this.status = status
    this.data = data
  }
}

export async function apiRequest<TResponse>(
  path: string,
  options: ApiRequestOptions = {}
): Promise<TResponse> {
  const headers = new Headers(options.headers)
  const body = createRequestBody(options.body, headers)

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    body,
    headers,
    credentials: "include",
  })

  if (response.status === 204) {
    return undefined as TResponse
  }

  const data = await parseResponseBody(response)

  if (!response.ok) {
    throw new ApiError(getErrorMessage(data, response), response.status, data)
  }

  return data as TResponse
}

function createRequestBody(body: unknown, headers: Headers) {
  if (body === undefined || body === null) {
    return undefined
  }

  if (body instanceof FormData || body instanceof Blob) {
    return body
  }

  if (!headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json")
  }

  return JSON.stringify(body)
}

async function parseResponseBody(response: Response) {
  const contentType = response.headers.get("Content-Type")

  if (contentType?.includes("application/json")) {
    return response.json()
  }

  return response.text()
}

function getErrorMessage(data: unknown, response: Response) {
  if (
    data &&
    typeof data === "object" &&
    "error" in data &&
    typeof data.error === "string"
  ) {
    return data.error
  }

  return response.statusText || "Erro inesperado."
}
