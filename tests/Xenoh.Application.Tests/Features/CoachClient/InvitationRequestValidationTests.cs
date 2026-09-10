using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xenoh.API.Controllers;
using Xunit;

namespace Xenoh.Application.Tests.Features.CoachClient;

public sealed class InvitationRequestValidationTests
{
    [Theory]
    [InlineData("FVPE7DCW", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void InvitationPreview_ValidatesCodeWithoutMetadataException(string? code, bool valid)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvc();
        using var provider = services.BuildServiceProvider();
        var context = new ActionContext(new DefaultHttpContext { RequestServices = provider },
            new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var validator = provider.GetRequiredService<IObjectModelValidator>();

        validator.Validate(context, null, string.Empty,
            new RelationshipsController.PreviewInvitationBody(code!));

        context.ModelState.IsValid.Should().Be(valid);
        if (!valid) context.ModelState.Should().ContainKey("Code");
    }
}
