#include "ShadowVeilGameMode.h"
#include "ShadowVeilPawn.h"
#include "ShadowVeilPlayerController.h"
#include "Engine/StaticMeshActor.h"
#include "UObject/ConstructorHelpers.h"

AShadowVeilGameMode::AShadowVeilGameMode()
{
	DefaultPawnClass = AShadowVeilPawn::StaticClass();
	PlayerControllerClass = AShadowVeilPlayerController::StaticClass();
}

void AShadowVeilGameMode::SpawnPrimitive(const TCHAR* MeshPath, const FVector& Location, const FVector& Scale, const FRotator& Rotation)
{
	static UStaticMesh* Cube = LoadObject<UStaticMesh>(nullptr, MeshPath);
	if (!Cube) return;
	AStaticMeshActor* Tile = GetWorld()->SpawnActor<AStaticMeshActor>(Location, Rotation);
	if (!Tile) return;
	Tile->GetStaticMeshComponent()->SetStaticMesh(Cube);
	Tile->GetStaticMeshComponent()->SetCollisionProfileName(TEXT("BlockAll"));
	Tile->SetActorScale3D(Scale);
}

void AShadowVeilGameMode::BeginPlay()
{
	Super::BeginPlay();
	// Long reusable forest trail: the follow camera scrolls with the hero as they walk.
	for (int32 Index = -2; Index < 18; ++Index)
	{
		SpawnPrimitive(TEXT("/Engine/BasicShapes/Cube.Cube"), FVector(0.0f, Index * 1200.0f, -45.0f), FVector(5.0f, 6.0f, 0.45f));
	}
	SpawnPrimitive(TEXT("/Engine/BasicShapes/Cube.Cube"), FVector(-570.0f, 9000.0f, 65.0f), FVector(0.65f, 100.0f, 1.25f));
	SpawnPrimitive(TEXT("/Engine/BasicShapes/Cube.Cube"), FVector(570.0f, 9000.0f, 65.0f), FVector(0.65f, 100.0f, 1.25f));
}
