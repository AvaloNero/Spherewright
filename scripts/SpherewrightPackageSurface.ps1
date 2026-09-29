Set-StrictMode -Version Latest

# Protocol-only checks. No Bridge request or gameplay action is sent by this policy.
function Assert-SpherewrightPackageSurface {
    param(
        [Parameter(Mandatory)][version]$Version,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Tools,
        [Parameter(Mandatory)][string]$PackagedPlaybook,
        [Parameter(Mandatory)][string]$ResourcePlaybook
    )

    $normalize = { param([string]$value) $value.Replace("`r`n", "`n").TrimStart([char]0xFEFF) }
    if (-not [string]::Equals((& $normalize $PackagedPlaybook), (& $normalize $ResourcePlaybook), [StringComparison]::Ordinal)) {
        throw 'The packaged playbook differs from the MCP embedded resource; rebuild the same source cohort.'
    }
    if ($Version -lt [version]'0.4.0') { return }

    $readOnlyTools = @(
        'spherewright_get_overseer_production',
        'spherewright_get_overseer_summary',
        'spherewright_get_overseer_diagnostic_bundle',
        'spherewright_get_foundry_plan',
        'spherewright_inspect_blueprint',
        'spherewright_export_blueprint',
        'spherewright_get_blueprint_builds',
        'spherewright_get_governor_plan'
    )
    $writeTools = @(
        'spherewright_prepare_upgrade', 'spherewright_commit_upgrade',
        'spherewright_prepare_blueprint_build', 'spherewright_commit_blueprint_build',
        'spherewright_prepare_cancel_blueprint', 'spherewright_commit_cancel_blueprint',
        'spherewright_prepare_select_research', 'spherewright_prepare_dismantle', 'spherewright_commit_dismantle'
    )
    foreach ($name in ($readOnlyTools + $writeTools)) {
        $matchingTools = @($Tools | Where-Object { $_.name -ceq $name })
        if ($matchingTools.Count -ne 1) { throw "The merged 0.4 package requires exactly one tool named $name." }
        $annotations = $matchingTools[0].PSObject.Properties['annotations']
        $readOnly = if ($annotations) { $annotations.Value.PSObject.Properties['readOnlyHint'] } else { $null }
        if (-not $readOnly -or $readOnly.Value -isnot [bool] -or $readOnly.Value -ne ($readOnlyTools -ccontains $name)) {
            throw "The package has missing/incorrect read-only metadata for $name."
        }
    }
    $research = @($Tools | Where-Object { $_.name -ceq 'spherewright_prepare_select_research' })[0]
    $schema = $research.PSObject.Properties['inputSchema']
    $properties = if ($schema) { $schema.Value.PSObject.Properties['properties'] } else { $null }
    $priority = if ($properties) { $properties.Value.PSObject.Properties['prioritizeQueued'] } else { $null }
    if (-not $priority -or $priority.Value.type -cne 'boolean') {
        throw 'The package lacks the explicit native research-priority option.'
    }
    $governor = @($Tools | Where-Object { $_.name -ceq 'spherewright_get_governor_plan' })[0]
    $governorSchema = $governor.PSObject.Properties['inputSchema']
    $governorProperties = if ($governorSchema) { $governorSchema.Value.PSObject.Properties['properties'] } else { $null }
    $declaration = if ($governorProperties) { $governorProperties.Value.PSObject.Properties['validationBaselineProposalHash'] } else { $null }
    $declarationType = if ($declaration) { $declaration.Value.PSObject.Properties['type'] } else { $null }
    $declarationTypes = @()
    if ($declarationType) { $declarationTypes = @($declarationType.Value) }
    $invalidDeclarationTypes = @($declarationTypes | Where-Object { $_ -cne 'string' -and $_ -cne 'null' }).Count -gt 0
    $duplicateDeclarationTypes = @($declarationTypes | Select-Object -Unique).Count -ne $declarationTypes.Count
    if (-not $declaration -or $declarationTypes.Count -lt 1 -or $declarationTypes.Count -gt 2 -or $declarationTypes -cnotcontains 'string' -or $invalidDeclarationTypes -or $duplicateDeclarationTypes) {
        throw 'The package lacks the immutable pre-execution Governor declaration option.'
    }
    foreach ($requirement in @(
        @('spherewright_get_foundry_plan', 'blueprint'),
        @('spherewright_prepare_blueprint_build', 'foundryIntent'),
        @('spherewright_prepare_blueprint_build', 'expectedFoundryPlanHash')
    )) {
        $tool = @($Tools | Where-Object { $_.name -ceq $requirement[0] })[0]
        $inputSchema = $tool.PSObject.Properties['inputSchema']
        $inputProperties = if ($inputSchema) { $inputSchema.Value.PSObject.Properties['properties'] } else { $null }
        if (-not $inputProperties -or -not $inputProperties.Value.PSObject.Properties[$requirement[1]]) {
            throw "The package lacks the Foundry finite-composition option: $($requirement[1])."
        }
    }
    $dismantle = @($Tools | Where-Object { $_.name -ceq 'spherewright_prepare_dismantle' })[0]
    $dismantleDescription = $dismantle.PSObject.Properties['description']
    if (-not $dismantleDescription -or ([string]$dismantleDescription.Value).IndexOf('2011/2012', [StringComparison]::Ordinal) -lt 0) {
        throw 'The package lacks the guarded basic-sorter dismantle contract.'
    }
    $foundry = @($Tools | Where-Object { $_.name -ceq 'spherewright_get_foundry_plan' })[0]
    $foundryDescription = $foundry.PSObject.Properties['description']
    if (-not $foundryDescription -or ([string]$foundryDescription.Value).IndexOf('transportBudget', [StringComparison]::Ordinal) -lt 0) {
        throw 'The package lacks the native Foundry transport budget contract.'
    }
    foreach ($rule in @('maximumObjectsToSubmit', 'resumeBuildId', 'never replay the whole blueprint',
            'spherewright_get_governor_plan', 'nonzero measured baseline', 'ten game minutes', 'researchQueueReadback',
            'validationBaselineProposalHash', 'inputEndpointFacingDot/outputEndpointFacingDot',
            'foundryIntent', 'expectedFoundryPlanHash', 'boundaryPorts', '2011/2012', 'transportBudget')) {
        if ($ResourcePlaybook.IndexOf($rule, [StringComparison]::Ordinal) -lt 0) {
            throw "The merged 0.4 playbook lacks required finite-construction/Governor rule: $rule."
        }
    }
}
