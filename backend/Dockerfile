# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HotelReservation.Api/*.csproj HotelReservation.Api/
COPY HotelReservation.Application/*.csproj HotelReservation.Application/
COPY HotelReservation.Domain/*.csproj HotelReservation.Domain/
COPY HotelReservation.Infrastructure/*.csproj HotelReservation.Infrastructure/
RUN dotnet restore HotelReservation.Api/HotelReservation.Api.csproj

COPY HotelReservation.Api/ HotelReservation.Api/
COPY HotelReservation.Application/ HotelReservation.Application/
COPY HotelReservation.Domain/ HotelReservation.Domain/
COPY HotelReservation.Infrastructure/ HotelReservation.Infrastructure/

RUN dotnet publish HotelReservation.Api/HotelReservation.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HotelReservation.Api.dll"]