using System.Security.Claims;
using UniFlowHub.Api.Dtos.Perfis;
using UniFlowHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniFlowHub.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/perfis")]
    public class PerfisController : ControllerBase
    {
        private readonly PerfisService _service;

        public PerfisController(PerfisService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            try { return Ok(await _service.ListAsync(GetRole())); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        }

        [HttpGet("acessos")]
        public async Task<IActionResult> ListAcessos()
        {
            return Ok(await _service.ListAcessosAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] PerfilSistemaSaveDto dto)
        {
            try { return Ok(await _service.SaveAsync(GetRole(), dto)); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(GetRole(), id);
                return NoContent();
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        }

        private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
