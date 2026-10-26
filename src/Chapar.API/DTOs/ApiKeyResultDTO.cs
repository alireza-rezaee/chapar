namespace Chapar.API.DTOs;

public record ApiKeyResultDTO
(
    string PlainApiKey,
    string KeyHash,
    string KeyPreview
);
