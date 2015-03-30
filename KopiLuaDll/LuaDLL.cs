//#if (!__solua__ && !__Windows__)
//  #define __liblua__
//#endif
namespace Lua511
{

	using System;
	using System.Runtime.InteropServices;
	using System.Reflection;
	using System.Collections;
	using System.Text;
    using System.Security;
    using KopiLua;

	/*
	 * Lua types for the API, returned by lua_type function
	 */
	public enum LuaTypes
	{
		LUA_TNONE=-1,
		LUA_TNIL=0,
		LUA_TNUMBER=3,
		LUA_TSTRING=4,
		LUA_TBOOLEAN=1,
		LUA_TTABLE=5,
		LUA_TFUNCTION=6,
		LUA_TUSERDATA=7,
		LUA_TLIGHTUSERDATA=2
	}

	// steffenj: BEGIN lua garbage collector options
	/*
	 * Lua Garbage Collector options (param "what")
	 */
	public enum LuaGCOptions
	{
		LUA_GCSTOP = 0,
		LUA_GCRESTART = 1,
		LUA_GCCOLLECT = 2,
		LUA_GCCOUNT = 3,
		LUA_GCCOUNTB = 4,
		LUA_GCSTEP = 5,
		LUA_GCSETPAUSE = 6,
		LUA_GCSETSTEPMUL = 7,
	}
		/*
	sealed class LuaGCOptions
	{
		public static int LUA_GCSTOP = 0;
		public static int LUA_GCRESTART = 1;
		public static int LUA_GCCOLLECT = 2;
		public static int LUA_GCCOUNT = 3;
		public static int LUA_GCCOUNTB = 4;
		public static int LUA_GCSTEP = 5;
		public static int LUA_GCSETPAUSE = 6;
		public static int LUA_GCSETSTEPMUL = 7;
	};
		 */
	// steffenj: END lua garbage collector options

	/*
	 * Special stack indexes
	 */
	public class LuaIndexes
	{
		public static int LUA_REGISTRYINDEX=-10000;
		public static int LUA_ENVIRONINDEX=-10001;	// steffenj: added environindex
		public static int LUA_GLOBALSINDEX=-10002;	// steffenj: globalsindex previously was -10001
	}

	/*
	 * Structure used by the chunk reader
	 */
	[ StructLayout( LayoutKind.Sequential )]
	public struct ReaderInfo
	{
		public String chunkData;
		public bool finished;
	}

	/*
	 * Delegate for functions passed to Lua as function pointers
	 */
	public delegate int LuaCSFunction(KopiLua.LuaState luaState);

	/*
	 * Delegate for chunk readers used with lua_load
	 */
	public delegate string LuaChunkReader(KopiLua.LuaState luaState,ref ReaderInfo data,ref uint size);


    /// <summary>
    /// Used to handle Lua panics
    /// </summary>
    /// <param name="luaState"></param>
    /// <returns></returns>
    public delegate int LuaFunctionCallback(KopiLua.LuaState luaState);

	/*
	 * P/Invoke wrapper of the Lua API
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 *
	 * // steffenj: noteable changes in the LuaDLL API:
	 * - luaopen_* functions are gone
	 *		(however Lua class constructor already calls luaL_openlibs now, so just remove these calls)
	 * - deprecated functions: lua_open, lua_strlen, lua_dostring
	 *		(they still work but may be removed with next Lua version)
	 *
	 * list of functions of the Lua 5.1.1 C API that are not in LuaDLL
	 * i thought this may come in handy for the next Lua version upgrade and for anyone to see
	 * what the differences are in the APIs (C API vs LuaDLL API)
		lua_concat			(use System.String concatenation or similar)
		lua_cpcall			(no point in calling C functions)
		lua_dump			(would write to unmanaged memory via lua_Writer)
		lua_getallocf		(no C functions/pointers)
		lua_isthread		(no threads)
		lua_newstate		(use luaL_newstate)
		lua_newthread		(no threads)
		lua_pushcclosure	(no C functions/pointers)
		lua_pushcfunction	(no C functions/pointers)
		lua_pushfstring		(use lua_pushstring)
		lua_pushthread		(no threads)
		lua_pushvfstring	(use lua_pushstring)
		lua_register		(all libs already opened, use require in scripts for external libs)
		lua_resume			(no threads)
		lua_setallocf		(no C functions/pointers)
		lua_status			(no threads)
		lua_tointeger		(use System.Convert)
		lua_tolstring		(use lua_tostring)
		lua_topointer		(no C functions/pointers)
		lua_tothread		(no threads)
		lua_xmove			(no threads)
		lua_yield			(no threads)

		luaL_add*			(use System.String concatenation or similar)
		luaL_argcheck		(function argument checking unnecessary)
		luaL_argerror		(function argument checking unnecessary)
		luaL_buffinit		(use System.String concatenation or similar)
		luaL_checkany		(function argument checking unnecessary)
		luaL_checkint		(function argument checking unnecessary)
		luaL_checkinteger	(function argument checking unnecessary)
		luaL_checklong		(function argument checking unnecessary)
		luaL_checklstring	(function argument checking unnecessary)
		luaL_checknumber	(function argument checking unnecessary)
		luaL_checkoption	(function argument checking unnecessary)
		luaL_checkstring	(function argument checking unnecessary)
		luaL_checktype		(function argument checking unnecessary)
		luaL_prepbuffer		(use System.String concatenation or similar)
		luaL_pushresult		(use System.String concatenation or similar)
		luaL_register		(all libs already opened, use require in scripts for external libs)
		luaL_typerror		(function argument checking unnecessary)

		(complete lua_Debug interface omitted)
		lua_gethook***
		lua_getinfo
		lua_getlocal
		lua_getstack
		lua_getupvalue
		lua_sethook
		lua_setlocal
		lua_setupvalue
	 */
	public class LuaDLL
	{
        // for debugging
        // const string BASEPATH = @"C:\development\software\dotnet\tools\PulseRecognizer\PulseRecognizer\bin\Debug\";
        // const string BASEPATH = @"C:\development\software\ThirdParty\lua\Built\";
        /*const string BASEPATH = "";
        #if __Windows__
        const string DLLX = ".dll";
        #else
        const string DLLX = ".so";
        #endif
        #if __lib__
        const string PREFIX = "liblua";
        #else
        const string PREFIX = "lua";
        #endif
        #if __novs__
        const string DLL = PREFIX;
        #elif __dot__ 
        const string DLL = PREFIX+"5.1";
        #else
        const string DLL = PREFIX+"51";
        #endif
		const string LUADLL = BASEPATH + DLL + DLLX;
		const string LUALIBDLL = LUADLL;
        #if __embed__
        const string STUBDLL = LUADLL;
        #else
        const string STUBDLL = BASEPATH + "luanet" + DLLX;
        #endif
        */
		// steffenj: BEGIN additional Lua API functions new in Lua 5.1
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static int lua_gc(KopiLua.LuaState luaState, LuaGCOptions what, int data)
        {
            return KopiLua.Lua.LuaGC(luaState, (int)what, data);
        }
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static string lua_typename(KopiLua.LuaState luaState, LuaTypes type)
        {
            return KopiLua.Lua.LuaTypeName(luaState, (int)type).ToString();
        }
        public static string luaL_typename(KopiLua.LuaState luaState, int stackPos)
		{
            return lua_typename(luaState, lua_type(luaState, stackPos));
		}

		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static void luaL_error(KopiLua.LuaState luaState, string message)
        {
            KopiLua.Lua.LuaLError(luaState, message);
        }

        public static void lua_error(KopiLua.LuaState luaState)
        {
            KopiLua.Lua.LuaError(luaState);
        }
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
        //Not wrapped
        //public static string luaL_gsub(KopiLua.LuaState luaState, string str, string pattern, string replacement);
#if false
		// the functions below are still untested
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static void lua_getfenv(KopiLua.LuaState luaState, int stackPos);
		public static int lua_isfunction(KopiLua.LuaState luaState, int stackPos);
		public static int lua_islightuserdata(KopiLua.LuaState luaState, int stackPos);
		public static int lua_istable(KopiLua.LuaState luaState, int stackPos);
		public static int lua_isuserdata(KopiLua.LuaState luaState, int stackPos);
		public static int lua_lessthan(KopiLua.LuaState luaState, int stackPos1, int stackPos2);
		public static int lua_rawequal(KopiLua.LuaState luaState, int stackPos1, int stackPos2);
		public static int lua_setfenv(KopiLua.LuaState luaState, int stackPos);
		public static void lua_setfield(KopiLua.LuaState luaState, int stackPos, string name);
		public static int luaL_callmeta(KopiLua.LuaState luaState, int stackPos, string name);
		// steffenj: END additional Lua API functions new in Lua 5.1
#endif
		// steffenj: BEGIN Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static KopiLua.LuaState luaL_newstate(){
            return KopiLua.Lua.LuaLNewState();
        }
		/// <summary>DEPRECATED - use luaL_newstate() instead!</summary>
        public static KopiLua.LuaState lua_open()
		{
			return LuaDLL.luaL_newstate();
		}
		// steffenj: END Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static void lua_close(KopiLua.LuaState luaState)
        {
            KopiLua.Lua.LuaClose(luaState);
        }
		// steffenj: BEGIN Lua 5.1.1 API change (new function luaL_openlibs)
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static void luaL_openlibs(KopiLua.LuaState luaState)
        {
            KopiLua.Lua.LuaLOpenLibs(luaState);
        }
		/*
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaopen_base(IntPtr luaState);
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_io(IntPtr luaState);
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_table(IntPtr luaState);
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_string(IntPtr luaState);
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_math(IntPtr luaState);
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_debug(IntPtr luaState);
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_loadlib(IntPtr luaState);
		 */
		// steffenj: END Lua 5.1.1 API change (new function luaL_openlibs)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static int lua_objlen(KopiLua.LuaState luaState, int stackPos)
        {
            return (int)KopiLua.Lua.LuaObjectLen(luaState, stackPos);
        }
		/// <summary>DEPRECATED - use lua_objlen(IntPtr luaState, int stackPos) instead!</summary>
        public static int lua_strlen(KopiLua.LuaState luaState, int stackPos)
		{
			return lua_objlen(luaState, stackPos);
		}
		// steffenj: END Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static int luaL_loadstring(KopiLua.LuaState luaState, string chunk)
        {
            return KopiLua.Lua.LuaLLoadString(luaState, chunk);
        }
        public static int luaL_dostring(KopiLua.LuaState luaState, string chunk)
		{
			int result = LuaDLL.luaL_loadstring(luaState, chunk);
			if (result != 0)
				return result;

			return LuaDLL.lua_pcall(luaState, 0, -1, 0);
		}
		/// <summary>DEPRECATED - use luaL_dostring(IntPtr luaState, string chunk) instead!</summary>
        public static int lua_dostring(KopiLua.LuaState luaState, string chunk)
		{
			return LuaDLL.luaL_dostring(luaState, chunk);
		}
		// steffenj: END Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static void lua_createtable(KopiLua.LuaState luaState, int narr, int nrec)
        {
            KopiLua.Lua.LuaCreateTable(luaState, narr, nrec);
        }
        public static void lua_newtable(KopiLua.LuaState luaState)
		{
			LuaDLL.lua_createtable(luaState, 0, 0);
		}
		// steffenj: END Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile macro)
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static int luaL_dofile(KopiLua.LuaState luaState, string fileName)
		{
			int result = LuaDLL.luaL_loadfile(luaState, fileName);
			if (result != 0)
				return result;

			return LuaDLL.lua_pcall(luaState, 0, -1, 0);
		}
		// steffenj: END Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile)
        public static void lua_getglobal(KopiLua.LuaState luaState, string name)
		{
			LuaDLL.lua_pushstring(luaState,name);
			LuaDLL.lua_gettable(luaState,LuaIndexes.LUA_GLOBALSINDEX);
		}
        public static void lua_setglobal(KopiLua.LuaState luaState, string name)
		{
			LuaDLL.lua_pushstring(luaState,name);
			LuaDLL.lua_insert(luaState,-2);
			LuaDLL.lua_settable(luaState,LuaIndexes.LUA_GLOBALSINDEX);
		}
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_settop(KopiLua.LuaState luaState, int newTop)
        {
            KopiLua.Lua.LuaSetTop(luaState, newTop);
        }
		// steffenj: BEGIN added lua_pop "macro"
		public static void lua_pop(KopiLua.LuaState luaState, int amount)
		{
			LuaDLL.lua_settop(luaState, -(amount) - 1);
		}
		// steffenj: END added lua_pop "macro"
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static void lua_insert(KopiLua.LuaState luaState, int newTop)
        {
            KopiLua.Lua.LuaInsert(luaState, newTop);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_remove(KopiLua.LuaState luaState, int index)
        {
            KopiLua.Lua.LuaRemove(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_gettable(KopiLua.LuaState luaState, int index)
        {
            KopiLua.Lua.LuaGetTable(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static void lua_rawget(KopiLua.LuaState luaState, int index)
        {
            KopiLua.Lua.LuaRawGet(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_settable(KopiLua.LuaState luaState, int index)
        {
            KopiLua.Lua.LuaSetTable(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_rawset(KopiLua.LuaState luaState, int index)
        {
            KopiLua.Lua.LuaRawSet(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_setmetatable(KopiLua.LuaState luaState, int objIndex)
        {
            KopiLua.Lua.LuaSetMetatable(luaState, objIndex);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static int lua_getmetatable(KopiLua.LuaState luaState, int objIndex)
        {
            return KopiLua.Lua.LuaGetMetatable(luaState, objIndex);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static int lua_equal(KopiLua.LuaState luaState, int index1, int index2)
        {
            return KopiLua.Lua.LuaEqual(luaState, index1, index2);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_pushvalue(KopiLua.LuaState luaState, int index)
        {
            KopiLua.Lua.LuaPushValue(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_replace(KopiLua.LuaState luaState, int index)
        {
            KopiLua.Lua.LuaReplace(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static int lua_gettop(KopiLua.LuaState luaState)
        {
            return KopiLua.Lua.LuaGetTop(luaState);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static LuaTypes lua_type(KopiLua.LuaState luaState, int index)
        {
            return (LuaTypes)KopiLua.Lua.LuaType(luaState, index);
        }
		public static bool lua_isnil(KopiLua.LuaState luaState, int index)
		{
			return (LuaDLL.lua_type(luaState,index)==LuaTypes.LUA_TNIL);
		}
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static bool lua_isnumber(KopiLua.LuaState luaState, int index)
        {
            return (LuaDLL.lua_type(luaState, index) == LuaTypes.LUA_TNUMBER);
        }
		public static bool lua_isboolean(KopiLua.LuaState luaState, int index)
		{
			return LuaDLL.lua_type(luaState,index)==LuaTypes.LUA_TBOOLEAN;
		}
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static int luaL_ref(KopiLua.LuaState luaState, int registryIndex)
        {
            return KopiLua.Lua.LuaLRef(luaState, registryIndex);
        }
		public static int lua_ref(KopiLua.LuaState luaState, int lockRef)
		{
			if(lockRef!=0)
			{
				return LuaDLL.luaL_ref(luaState,LuaIndexes.LUA_REGISTRYINDEX);
			}
			else return 0;
		}
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_rawgeti(KopiLua.LuaState luaState, int tableIndex, int index)
        {
            KopiLua.Lua.LuaRawGetI(luaState, tableIndex, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_rawseti(KopiLua.LuaState luaState, int tableIndex, int index)
        {
            KopiLua.Lua.LuaRawSetI(luaState, tableIndex, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static object lua_newuserdata(KopiLua.LuaState luaState, int size)
        {
            return KopiLua.Lua.LuaNewUserData(luaState, (uint)size);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static object lua_touserdata(KopiLua.LuaState luaState, int index)
        {
            return KopiLua.Lua.LuaToUserData(luaState, index);
        }
		public static void lua_getref(KopiLua.LuaState luaState, int reference)
		{
			LuaDLL.lua_rawgeti(luaState,LuaIndexes.LUA_REGISTRYINDEX,reference);
		}
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static void luaL_unref(KopiLua.LuaState luaState, int registryIndex, int reference)
        {
            KopiLua.Lua.LuaLUnref(luaState, registryIndex, reference);
        }
		public static void lua_unref(KopiLua.LuaState luaState, int reference)
		{
			LuaDLL.luaL_unref(luaState,LuaIndexes.LUA_REGISTRYINDEX,reference);
		}
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static bool lua_isstring(KopiLua.LuaState luaState, int index)
        {
            return KopiLua.Lua.LuaIsString(luaState, index) != 0;
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static bool lua_iscfunction(KopiLua.LuaState luaState, int index)
        {
            return KopiLua.Lua.LuaIsCFunction(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_pushnil(KopiLua.LuaState luaState)
        {
            KopiLua.Lua.LuaPushNil(luaState);
        }
		//[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_pushstdcallcfunction(KopiLua.LuaState luaState, KopiLua.LuaNativeFunction function)
        {
            KopiLua.Lua.LuaPushCFunction(luaState, function);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_call(KopiLua.LuaState luaState, int nArgs, int nResults)
        {
            KopiLua.Lua.LuaCall(luaState, nArgs, nResults);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static int lua_pcall(KopiLua.LuaState luaState, int nArgs, int nResults, int errfunc)
        {
            return KopiLua.Lua.LuaPCall(luaState, nArgs, nResults, errfunc);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        //not wrapped
        //public static extern int lua_rawcall(KopiLua.LuaState luaState, int nArgs, int nResults)
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static KopiLua.LuaNativeFunction lua_tocfunction(KopiLua.LuaState luaState, int index)
        {
            return KopiLua.Lua.LuaToCFunction(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static double lua_tonumber(KopiLua.LuaState luaState, int index)
        {
            return KopiLua.Lua.LuaToNumber(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static bool lua_toboolean(KopiLua.LuaState luaState, int index)
        {
            return KopiLua.Lua.LuaToBoolean(luaState, index) != 0;
        }

		//[DllImport(LUADLL,CallingConvention = CallingConvention.Cdecl)]
        //unwrapped
        //public static IntPtr lua_tolstring(KopiLua.LuaState luaState, int index, out int strLen)

        public static string lua_tostring(KopiLua.LuaState luaState, int index)
        {
        #if true
            // FIXME use the same format string as lua i.e. LUA_NUMBER_FMT
            LuaTypes t = lua_type(luaState, index);

            if (t == LuaTypes.LUA_TNUMBER)
                return string.Format("{0}", lua_tonumber(luaState, index));
            else if (t == LuaTypes.LUA_TSTRING)
            {
                uint strlen;
                return KopiLua.Lua.LuaToLString(luaState, index, out strlen).ToString();
            }
            else if (t == LuaTypes.LUA_TNIL)
                return null;			// treat lua nulls to as C# nulls
            else
                return "0";	// Because luaV_tostring does this
        #else


			size_t strlen;

			// Note!  This method will _change_ the representation of the object on the stack to a string.
			// We do not want this behavior so we do the conversion ourselves
			const char *str = Lua.lua_tolstring(luaState, index, &strlen);
            if (str)
				return Marshal::PtrToStringAnsi(IntPtr((char *) str), strlen);
            else
                return nullptr;            // treat lua nulls to as C# nulls
        #endif
        }


        //[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static void lua_atpanic(KopiLua.LuaState luaState, KopiLua.LuaNativeFunction panicf)
        {
            KopiLua.Lua.LuaAtPanic(luaState, panicf);
        }

		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_pushnumber(KopiLua.LuaState luaState, double number)
        {
            KopiLua.Lua.LuaPushNumber(luaState, number);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_pushboolean(KopiLua.LuaState luaState, bool value)
        {
            KopiLua.Lua.LuaPushBoolean(luaState, value?1:0);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        // Not yet wrapped
        //public static void lua_pushlstring(KopiLua.LuaState luaState, string str, int size);
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_pushstring(KopiLua.LuaState luaState, string str)
        {
            KopiLua.Lua.LuaPushString(luaState, str);
        }
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static int luaL_newmetatable(KopiLua.LuaState luaState, string meta)
        {
            return KopiLua.Lua.LuaLNewMetatable(luaState, meta);
        }
		// steffenj: BEGIN Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static void lua_getfield(KopiLua.LuaState luaState, int stackPos, string meta)
        {
            KopiLua.Lua.LuaGetField(luaState, stackPos, meta);
        }
		public static void luaL_getmetatable(KopiLua.LuaState luaState, string meta)
		{
			LuaDLL.lua_getfield(luaState, LuaIndexes.LUA_REGISTRYINDEX, meta);
		}
		// steffenj: END Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static Object luaL_checkudata(KopiLua.LuaState luaState, int stackPos, string meta)
        {
            return KopiLua.Lua.LuaLCheckUData(luaState, stackPos, meta);
        }
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static bool luaL_getmetafield(KopiLua.LuaState luaState, int stackPos, string field)
        {
            return KopiLua.Lua.LuaLGetMetafield(luaState, stackPos, field) != 0;
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		//not yet wrapped
        //public static extern int lua_load(KopiLua.LuaState luaState, LuaChunkReader chunkReader, ref ReaderInfo data, string chunkName);
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static int luaL_loadbuffer(KopiLua.LuaState luaState, string buff, int size, string name)
        {
            return KopiLua.Lua.LuaLLoadBuffer(luaState, buff, (uint)size, name);
        }
		//[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static int luaL_loadfile(KopiLua.LuaState luaState, string filename)
        {
            return KopiLua.Lua.LuaLLoadFile(luaState, filename);
        }
		//[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static bool luaL_checkmetatable(KopiLua.LuaState luaState, int index)
        {
            bool retVal = false;

            if (lua_getmetatable(luaState, index) != 0)
            {
                lua_pushlightuserdata(luaState, 0);
                lua_rawget(luaState, -2);
                retVal = !lua_isnil(luaState, -1);
                lua_settop(luaState, -3);
            }
            return retVal;
        }

		//[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static void luanet_newudata(KopiLua.LuaState luaState, int val)
        {
            byte[] userdata = lua_newuserdata(luaState, sizeof(int)) as byte[];
            intToFourBytes(val, userdata);
        }
        //[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static int luanet_tonetobject(KopiLua.LuaState luaState, int index)
        {
            byte[] udata;

            if (lua_type(luaState, index) == LuaTypes.LUA_TUSERDATA)
            {
                if (luaL_checkmetatable(luaState, index))
                {
                    udata = lua_touserdata(luaState, index) as byte[];
                    if (udata != null)
                    {
                        return fourBytesToInt(udata);
                    }
                }

                udata = checkudata_raw(luaState, index, "luaNet_class") as byte[];
                if (udata != null) return fourBytesToInt(udata);

                udata = checkudata_raw(luaState, index, "luaNet_searchbase") as byte[];
                if (udata != null) return fourBytesToInt(udata);

                udata = checkudata_raw(luaState, index, "luaNet_function") as byte[];
                if (udata != null) return fourBytesToInt(udata);
            }
            return -1;
        }

		//[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static int luanet_rawnetobj(KopiLua.LuaState luaState, int obj)
        {
            byte[] bytes = lua_touserdata(luaState, obj) as byte[];
            return fourBytesToInt(bytes);
        }

		//[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static int luanet_checkudata(KopiLua.LuaState luaState, int ud, string tname)
        {
            object udata = checkudata_raw(luaState, ud, tname);

            if (udata != null) return fourBytesToInt(udata as byte[]);
            return -1;
        }

		//[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static bool lua_checkstack(KopiLua.LuaState luaState, int extra)
        {
            return KopiLua.Lua.LuaCheckStack(luaState, extra) != 0;
        }

		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static int lua_next(KopiLua.LuaState luaState, int index)
        {
            return KopiLua.Lua.LuaNext(luaState, index);
        }
		//[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void lua_pushlightuserdata(KopiLua.LuaState luaState, Object udata)
        {
            KopiLua.Lua.LuaPushLightUserData(luaState, udata);
        }
 		//[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
        public static Object luanet_gettag()
        {
            return 0;
        }
        //[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static void luaL_where(KopiLua.LuaState luaState, int level)
        {
            KopiLua.Lua.LuaLWhere(luaState, level);
        }

        private static int fourBytesToInt(byte[] bytes)
        {
            return bytes[0] + (bytes[1] << 8) + (bytes[2] << 16) + (bytes[3] << 24);
        }

        private static void intToFourBytes(int val, byte[] bytes)
        {
			// gfoot: is this really a good idea?
            bytes[0] = (byte)val;
            bytes[1] = (byte)(val >> 8);
            bytes[2] = (byte)(val >> 16);
            bytes[3] = (byte)(val >> 24);
        }
        private static object checkudata_raw(KopiLua.LuaState L, int ud, string tname)
        {
            object p = KopiLua.Lua.LuaToUserData(L, ud);

            if (p != null)
            {  /* value is a userdata? */
                if (KopiLua.Lua.LuaGetMetatable(L, ud) != 0)
                {
                    bool isEqual;

                    /* does it have a metatable? */
                    KopiLua.Lua.LuaGetField(L, (int)LuaIndexes.LUA_REGISTRYINDEX, tname);  /* get correct metatable */

                    isEqual = KopiLua.Lua.LuaRawEqual(L, -1, -2) != 0;

                    // NASTY - we need our own version of the lua_pop macro
                    // lua_pop(L, 2);  /* remove both metatables */
                    KopiLua.Lua.LuaSetTop(L, -(2) - 1);

                    if (isEqual)   /* does it have the correct mt? */
                        return p;
                }
            }

            return null;
        }

	}
}
