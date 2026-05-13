namespace OpenBase.CLI.Commands;

public sealed partial class ScaffoldGenerator
{
    private IEnumerable<(string, string)> PresentationFiles()
    {
        yield return (
            Path.Combine(ctx.PresentationPath, "Controllers", $"{ctx.Entity}Controller.cs"),
            ControllerTemplate());
    }

    private string ControllerTemplate() => $$"""
        using Microsoft.AspNetCore.Mvc;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.Interfaces.Services;

        namespace {{ctx.NS}}.Presentation.Api.Controllers;

        [ApiController]
        [Route("api/{{ctx.ELower}}")]
        [Produces("application/json")]
        public class {{ctx.Entity}}Controller(I{{ctx.Entity}}ApplicationService {{ctx.ECamel}}ApplicationService)
            : ControllerBase
        {
            /// <summary>Cria um(a) {{ctx.Entity}}.</summary>
            [HttpPost]
            [ProducesResponseType(StatusCodes.Status201Created)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
            public async Task<IActionResult> CreateAsync(
                [FromBody] Create{{ctx.Entity}}Request request,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.CreateAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = result!.Id }, result);
            }

            /// <summary>Remove um(a) {{ctx.Entity}}.</summary>
            [HttpDelete("{id:int}")]
            [ProducesResponseType(StatusCodes.Status204NoContent)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            public async Task<IActionResult> DeleteAsync(
                [FromRoute] int id,
                CancellationToken cancellationToken = default)
            {
                var request = new Delete{{ctx.Entity}}Request(id);
                var result = await {{ctx.ECamel}}ApplicationService.DeleteAsync(request, cancellationToken);

                if (result is null)
                    return NotFound();

                return NoContent();
            }

            /// <summary>Atualiza um(a) {{ctx.Entity}}.</summary>
            [HttpPut("{id:int}")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
            public async Task<IActionResult> UpdateAsync(
                [FromRoute] int id,
                [FromBody] Update{{ctx.Entity}}Request request,
                CancellationToken cancellationToken = default)
            {
                var requestWithId = request with { Id = id };
                var result = await {{ctx.ECamel}}ApplicationService.UpdateAsync(requestWithId, cancellationToken);

                if (result is null)
                    return NotFound();

                return Ok(result);
            }

            /// <summary>Lista {{ctx.EPlural}} com paginação.</summary>
            [HttpGet]
            [ProducesResponseType(StatusCodes.Status200OK)]
            public async Task<IActionResult> GetAsync(
                [FromQuery] Get{{ctx.Entity}}Request request,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.GetAsync(request, cancellationToken);
                return Ok(result);
            }

            /// <summary>Busca um(a) {{ctx.Entity}} pelo id.</summary>
            [HttpGet("{id:int}")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            public async Task<IActionResult> GetByIdAsync(
                [FromRoute] int id,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.GetByIdAsync(
                    new Find{{ctx.Entity}}ByIdRequest(id), cancellationToken);

                if (result is null)
                    return NotFound();

                return Ok(result);
            }
        }
        """;
}
