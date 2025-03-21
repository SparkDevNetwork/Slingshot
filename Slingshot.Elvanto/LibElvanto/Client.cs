using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LibElvanto.Attributes;
using LibElvanto.Contracts;

namespace LibElvanto;

public class Client
{
    private readonly string authValue;
    public Client( string apiKey )
    {
        authValue = Convert.ToBase64String( System.Text.ASCIIEncoding.ASCII.GetBytes( $"{apiKey}:x" ) );
    }

    public async IAsyncEnumerable<T> Get<T>( List<string>? fields = null ) where T : ElvantoContract
    {
        ElvantoResourceAttribute? elvantoResource = ( ElvantoResourceAttribute? ) Attribute.GetCustomAttribute( typeof( T ), typeof( ElvantoResourceAttribute ) );

        if ( elvantoResource == null )
        {
            throw new Exception( $"Type: {typeof( T ).Name} is not decorated with ElvantoResourceAttriubte" );
        }

        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Basic", authValue );

        var page = 1;

        fields = fields ?? new List<string>();
        fields.AddRange( elvantoResource.Fields );

        var fieldsString = GetFieldsString( fields );

        if ( typeof( T ) == typeof( Transaction ) )
        {
            fieldsString += $"&start=2000-01-01&end={DateTime.Today:yyyy-MM-dd}";
        }

        do
        {
            var results = await client.GetAsync( $"{elvantoResource.Url}?page={page}{fieldsString}" );
            if ( results == null )
            {
                throw new Exception( "Bad request" );
            }

            var content = await results.Content.ReadAsStringAsync();

            var paginatedResponse = ParseContent<T>( content, elvantoResource, fields );

            foreach ( var item in paginatedResponse.Data )
            {
                yield return item;
            }

            if ( paginatedResponse.Total < page * paginatedResponse.PerPage )
            {
                break;
            }

            page++;
        }
        while ( true );
    }

    private PaginatedResponse<T> ParseContent<T>(
        string content,
        ElvantoResourceAttribute elvantoResource,
        List<string> fields ) where T : ElvantoContract
    {
        var document = JsonDocument.Parse( content );

        if ( document == null )
        {
            throw new Exception( "Could not parse JSON" );
        }

        var response = new PaginatedResponse<T>();

        var root = document.RootElement;
        root.TryGetProperty( elvantoResource.PluralName, out var plural );


        if ( plural.TryGetProperty( "total", out var totalElement ) && totalElement.ValueKind == JsonValueKind.Number && totalElement.TryGetInt32( out var total ) )
        {
            response.Total = total;
        }

        if ( plural.TryGetProperty( "page", out var pageElement ) && pageElement.ValueKind == JsonValueKind.Number && pageElement.TryGetInt32( out var page ) )
        {
            response.Page = page;
        }

        if ( plural.TryGetProperty( "per_page", out var perpageElement ) && perpageElement.ValueKind == JsonValueKind.Number && perpageElement.TryGetInt32( out var perpage ) )
        {
            response.PerPage = perpage;
        }

        if ( plural.TryGetProperty( "on_this_page", out var onthisPageElement ) && onthisPageElement.ValueKind == JsonValueKind.Number && onthisPageElement.TryGetInt32( out var onthisPage ) )
        {
            response.OnThisPage = onthisPage;
        }

        if ( plural.TryGetProperty( elvantoResource.SingleName, out var dataset ) )
        {
            var dataElements = dataset.EnumerateArray();
            foreach ( var dataElement in dataElements )
            {
                var item = dataElement.Deserialize<T>();

                if ( item == null )
                {
                    continue;
                }

                response.Data.Add( item );

                item.Process( dataElement, fields );

            }
        }

        return response;
    }

    private string GetFieldsString( List<string> fields )
    {
        StringBuilder stringBuilder = new StringBuilder();
        for ( var i = 0; i < fields.Count; i++ )
        {
            stringBuilder.Append( $"&fields[{i}]={fields[i]}" );
        }
        return stringBuilder.ToString();
    }

}