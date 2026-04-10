---
name: python-development
description: Python development best practices including project structure, async patterns, testing, and design patterns. Use when building Python applications, setting up projects, or following Python best practices.
---

# Python Development

Comprehensive guide to Python development including project structure, async patterns, testing, design patterns, and best practices for production applications.

## When to Use This Skill

- Building Python applications from scratch
- Setting up new Python projects
- Implementing async patterns
- Writing tests for Python code
- Applying design patterns
- Configuring Python environments

## Core Concepts

### 1. Project Structure

**Recommended Layout:**

```
myproject/
├── src/
│   └── myproject/
│       ├── __init__.py
│       ├── services/
│       ├── models/
│       └── api/
├── tests/
├── pyproject.toml
└── README.md
```

### 2. Async Patterns

Proper async/await usage:

- Async route handlers
- Async database operations
- Async background tasks
- Concurrent execution with asyncio.gather

### 3. Dependency Management

Use `pyproject.toml` for modern Python packaging:

```toml
[project]
name = "myproject"
version = "0.1.0"
dependencies = [
    "fastapi>=0.100.0",
    "uvicorn>=0.23.0",
]

[project.optional-dependencies]
dev = [
    "pytest>=7.0.0",
    "pytest-asyncio>=0.21.0",
]

[build-system]
requires = ["setuptools>=61.0"]
build-backend = "setuptools.build_meta"
```

## Design Patterns

### KISS (Keep It Simple)

Choose the simplest solution that works:

```python
# Over-engineered: Factory with registration
class OutputFormatterFactory:
    _formatters: dict[str, type] = {}

    @classmethod
    def register(cls, name: str):
        def decorator(formatter_cls):
            cls._formatters[name] = formatter_cls
            return formatter_cls
        return decorator

    @classmethod
    def create(cls, name: str):
        return cls._formatters[name]()

# Simple: Just use a dictionary
FORMATTERS = {
    "json": JsonFormatter,
    "csv": CsvFormatter,
}

def get_formatter(name: str):
    if name not in FORMATTERS:
        raise ValueError(f"Unknown format: {name}")
    return FORMATTERS[name]()
```

### Single Responsibility Principle

Each class or function should have one reason to change:

```python
# BAD: Handler does everything
class UserHandler:
    async def create_user(self, request: Request) -> Response:
        # HTTP parsing
        data = await request.json()
        
        # Validation
        if not data.get("email"):
            raise ValueError("Email required")
        
        # Business logic
        user = await self.db.create_user(data)
        
        # Email sending
        await self.email.send_welcome(user)
        
        # Logging
        self.logger.info(f"User created: {user.id}")
        
        return Response(json=user)

# GOOD: Separated concerns
class UserService:
    async def create_user(self, data: dict) -> User:
        return await self.repository.create(data)

class UserController:
    async def create_user(self, request: Request) -> Response:
        data = await request.json()
        validator.validate(data)
        user = await self.user_service.create_user(data)
        return Response(json=user)
```

### Composition Over Inheritance

Build behavior by combining objects:

```python
# Instead of deep inheritance
class Animal:
    def eat(self): pass

class Mammal(Animal):
    def breathe(self): pass

class Dog(Mammal):
    def bark(self): pass

# Use composition
class Logger:
    def log(self, message): pass

class Database:
    def save(self, data): pass

class UserService:
    def __init__(self, logger: Logger, database: Database):
        self.logger = logger
        self.database = database
```

## Async Patterns

### Concurrent Execution

```python
import asyncio

async def fetch_user(user_id: int) -> dict:
    # Simulate API call
    await asyncio.sleep(0.1)
    return {"id": user_id, "name": f"User {user_id}"}

async def fetch_all_users(user_ids: list[int]) -> list[dict]:
    # Execute concurrently
    tasks = [fetch_user(uid) for uid in user_ids]
    return await asyncio.gather(*tasks)

# Usage
users = await fetch_all_users([1, 2, 3, 4, 5])
```

### Async Context Managers

```python
class DatabaseConnection:
    async def __aenter__(self):
        self.conn = await connect_to_db()
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        await self.conn.close()

async def main():
    async with DatabaseConnection() as conn:
        await conn.execute("SELECT * FROM users")
```

### Task Management

```python
async def process_items(items: list[str]):
    results = []
    
    # Create tasks
    tasks = [process_item(item) for item in items]
    
    # Wait for all with timeout
    try:
        results = await asyncio.wait_for(
            asyncio.gather(*tasks),
            timeout=30.0
        )
    except asyncio.TimeoutError:
        # Handle timeout
        pass
    
    return results
```

## Testing Patterns

### Fixtures and Dependency Injection

```python
import pytest
from unittest.mock import AsyncMock

@pytest.fixture
def mock_db():
    return AsyncMock()

@pytest.fixture
def user_service(mock_db):
    return UserService(repository=mock_db)

@pytest.mark.asyncio
async def test_create_user(user_service, mock_db):
    user = await user_service.create_user({"email": "test@example.com"})
    
    mock_db.create.assert_called_once()
    assert user.email == "test@example.com"
```

### Mocking External Dependencies

```python
from unittest.mock import AsyncMock, patch

@pytest.mark.asyncio
async def test_external_api_call():
    with patch("httpx.AsyncClient") as mock_client:
        mock_response = AsyncMock()
        mock_response.json.return_value = {"status": "ok"}
        
        client = AsyncMock()
        client.get.return_value = mock_response
        mock_client.return_value.__aenter__.return_value = client
        
        result = await fetch_data("http://api.example.com")
        assert result == {"status": "ok"}
```

### Parametrized Tests

```python
@pytest.mark.parametrize("input,expected", [
    ("hello", "HELLO"),
    ("world", "WORLD"),
    ("test", "TEST"),
])
def test_uppercase(input, expected):
    assert input.upper() == expected
```

## Error Handling

### Custom Exceptions

```python
class AppError(Exception):
    """Base exception for application errors."""
    def __init__(self, message: str, code: str = "APP_ERROR"):
        self.message = message
        self.code = code
        super().__init__(message)

class ValidationError(AppError):
    def __init__(self, message: str):
        super().__init__(message, "VALIDATION_ERROR")

class NotFoundError(AppError):
    def __init__(self, resource: str, id: str):
        super().__init__(f"{resource} {id} not found", "NOT_FOUND")
```

### Error Handling Patterns

```python
async def safe_operation():
    try:
        result = await risky_operation()
        return Result.success(result)
    except ValidationError as e:
        return Result.failure(e.code, e.message)
    except Exception as e:
        logger.exception("Unexpected error")
        return Result.failure("INTERNAL_ERROR", str(e))
```

## Configuration Management

### Environment-Based Configuration

```python
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    DATABASE_URL: str
    SECRET_KEY: str
    DEBUG: bool = False
    API_V1_STR: str = "/api/v1"
    
    class Config:
        env_file = ".env"
        case_sensitive = True

settings = Settings()
```

### Logging Configuration

```python
import logging
import sys

def setup_logging(level: str = "INFO"):
    logging.basicConfig(
        level=getattr(logging, level.upper()),
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
        handlers=[
            logging.StreamHandler(sys.stdout),
            logging.FileHandler("app.log"),
        ]
    )
```

## Performance Optimization

### Caching with functools

```python
from functools import lru_cache

@lru_cache(maxsize=128)
def expensive_function(arg: str) -> str:
    # Computation that benefits from caching
    return compute_result(arg)
```

### Lazy Evaluation

```python
from functools import cached_property

class DataProcessor:
    @cached_property
    def expensive_data(self):
        """Computed once and cached."""
        return self.load_data()
```

## Best Practices Summary

1. **Keep it simple** - Prefer straightforward solutions
2. **Single responsibility** - Each component does one thing well
3. **Use composition** - Prefer object composition over inheritance
4. **Write tests** - Test behavior, not implementation
5. **Handle errors explicitly** - Use custom exceptions
6. **Configure via environment** - Keep config out of code
7. **Use type hints** - Improve code readability and catch errors
8. **Document decisions** - Explain why, not just what
9. **Profile before optimizing** - Measure, don't guess
10. **Use virtual environments** - Isolate dependencies